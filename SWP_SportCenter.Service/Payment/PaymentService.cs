/* CODE CŨ B39: xác nhận không kiểm tra quyền/trạng thái và gán ngẫu nhiên lễ tân.
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Security;

namespace SWP_SportCenter.Service.Payment;

public class PaymentService : IPaymentService
{
    private readonly AppDbContext _dbContext;
    private readonly CurrentActor _actor;

    public PaymentService(AppDbContext dbContext, CurrentActor actor)
    {
        _dbContext = dbContext;
        _actor = actor;
    }

    // FIX B38: Đăng ký và thanh toán gói tập trực tuyến cho Member
    public async Task<Response.CheckoutResponse> CheckoutAsync(Request.CheckoutRequest request)
    {
        var memberId = await _actor.MemberIdAsync();

        var package = await _dbContext.MembershipPackages
            .FirstOrDefaultAsync(p => p.Id == request.MembershipPackageId && !p.IsDeleted);

        if (package == null)
        {
            throw new KeyNotFoundException("Gói tập không tồn tại.");
        }

        var today = DateTime.UtcNow.Date;
        var start = request.StartDate.Date < today ? today : request.StartDate.Date;
        var end = start.AddDays(package.DurationDays - 1);

        // 1. Tạo Membership ở trạng thái Inactive (chờ thanh toán kích hoạt)
        var membership = new Repository.Entity.Membership
        {
            Id = Guid.NewGuid(),
            MemberId = memberId,
            PackageId = package.Id,
            StartDate = start,
            EndDate = end,
            Status = MembershipStatus.Inactive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Memberships.Add(membership);

        // 2. Gán Lễ tân phụ trách hệ thống (hoặc bất kỳ lễ tân đang hoạt động để thỏa mãn FK)
        var receptionistId = await _dbContext.Receptionists
            .Where(r => !r.IsDeleted && !r.Account.IsDeleted)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync()
            ?? await _dbContext.Receptionists.Select(r => (Guid?)r.Id).FirstOrDefaultAsync()
            ?? Guid.Empty;

        var paymentMethod = Enum.IsDefined(typeof(PaymentMethod), request.PaymentMethod)
            ? (PaymentMethod)request.PaymentMethod
            : PaymentMethod.BankTransfer;

        // 3. Tạo Invoice với trạng thái Pending
        var invoice = new Repository.Entity.Invoice
        {
            Id = Guid.NewGuid(),
            ReceptionistId = receptionistId,
            MemberMembershipId = membership.Id,
            Membership = membership,
            Amount = package.Price,
            PaymentMethod = paymentMethod,
            PaymentDate = DateTimeOffset.UtcNow,
            Status = InvoiceStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Invoices.Add(invoice);

        await _dbContext.SaveChangesAsync();

        // 4. Sinh URL thanh toán (kết nối cổng thanh toán hoặc gateway mô phỏng)
        string? paymentUrl = null;
        if (!string.IsNullOrWhiteSpace(request.ReturnUrl))
        {
            paymentUrl = $"/api/payments/mock-gateway?paymentId={invoice.Id}&returnUrl={Uri.EscapeDataString(request.ReturnUrl)}";
        }

        return new Response.CheckoutResponse
        {
            PaymentId = invoice.Id,
            MembershipId = membership.Id,
            InvoiceId = invoice.Id,
            Amount = invoice.Amount,
            Status = (int)invoice.Status, // 1 = Pending
            PaymentUrl = paymentUrl
        };
    }

    // FIX B38: Xem chi tiết giao dịch thanh toán (Member chỉ xem của chính mình)
    public async Task<Response.PaymentDetailResponse> GetPaymentAsync(Guid paymentId)
    {
        var invoice = await _dbContext.Invoices
            .Include(i => i.Membership)
                .ThenInclude(m => m.Package)
            .FirstOrDefaultAsync(i => i.Id == paymentId);

        if (invoice == null)
        {
            throw new KeyNotFoundException("Không tìm thấy giao dịch thanh toán.");
        }

        if (_actor.IsMember)
        {
            var currentMemberId = await _actor.MemberIdAsync();
            if (invoice.Membership?.MemberId != currentMemberId)
            {
                throw new UnauthorizedAccessException("Bạn chỉ được xem giao dịch của chính mình.");
            }
        }

        int paymentStatus = invoice.Status switch
        {
            InvoiceStatus.Pending => 1,
            InvoiceStatus.Paid => 2,
            InvoiceStatus.Cancelled => 4,
            _ => 1
        };

        return new Response.PaymentDetailResponse
        {
            PaymentId = invoice.Id,
            Status = paymentStatus,
            Amount = invoice.Amount,
            PackageName = invoice.Membership?.Package?.PackageName ?? string.Empty,
            MembershipId = invoice.MemberMembershipId,
            InvoiceId = invoice.Id,
            PaidAt = invoice.Status == InvoiceStatus.Paid ? invoice.PaymentDate : null
        };
    }

    // FIX B38: Xác nhận thanh toán thành công (Webhook hoặc cổng thanh toán gọi)
    public async Task<Response.PaymentDetailResponse> ConfirmPaymentAsync(Guid paymentId)
    {
        var invoice = await _dbContext.Invoices
            .Include(i => i.Membership)
                .ThenInclude(m => m.Package)
            .FirstOrDefaultAsync(i => i.Id == paymentId);

        if (invoice == null)
        {
            throw new KeyNotFoundException("Không tìm thấy giao dịch thanh toán.");
        }

        invoice.Status = InvoiceStatus.Paid;
        invoice.PaymentDate = DateTimeOffset.UtcNow;
        if (invoice.Membership != null)
        {
            invoice.Membership.Status = MembershipStatus.Active;
        }

        await _dbContext.SaveChangesAsync();

        return new Response.PaymentDetailResponse
        {
            PaymentId = invoice.Id,
            Status = 2, // Paid
            Amount = invoice.Amount,
            PackageName = invoice.Membership?.Package?.PackageName ?? string.Empty,
            MembershipId = invoice.MemberMembershipId,
            InvoiceId = invoice.Id,
            PaidAt = invoice.PaymentDate
        };
    }
}
*/
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Security;
using SWP_SportCenter.Service.Exceptions;

namespace SWP_SportCenter.Service.Payment;

// FIX B39: checkout online có TTL, không dùng GET/returnUrl làm bằng chứng đã trả tiền.
// CODE CŨ: PaymentService(AppDbContext db, CurrentActor actor, TimeProvider? clock = null)
// FIX SEPAY: nối checkout với service QR/webhook, không ảnh hưởng thanh toán nội bộ khi module chưa bật.
// FIX B53 B55: commerce tính giá và khóa đơn trùng cùng transaction.
public class PaymentService(AppDbContext db, CurrentActor actor, Sepay.ISepayService sepay, ClassCommerceService commerce, TimeProvider? clock = null) : IPaymentService
{
    private DateTimeOffset? _now;
    private DateTimeOffset Now => _now ??= (clock ?? TimeProvider.System).GetUtcNow(); // FIX B55: snapshot thời gian request.
    public async Task<Response.CheckoutResponse> CheckoutAsync(Request.CheckoutRequest request)
    {
        var memberId = await actor.MemberIdAsync();
        var package = await db.MembershipPackages.FirstOrDefaultAsync(p => p.Id == request.MembershipPackageId && !p.IsDeleted);
        if (package == null) throw new KeyNotFoundException("Gói tập không tồn tại.");
        if (package.Status != MembershipStatus.Active || package.DurationDays <= 0 || package.Price <= 0)
            throw new BusinessException("Gói không khả dụng hoặc giá/thời hạn không hợp lệ.");
        if (request.PaymentMethod is not (2 or 3)) throw new BusinessException("Chỉ hỗ trợ chuyển khoản hoặc thẻ.");
        // FIX B55: không tạo thêm đơn gói/nâng cấp khi đang có đơn chờ.
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        await commerce.EnsureNoPackagePending(memberId);
        // CODE CŨ: Now.UtcDateTime.Date.
        var today = DateTime.SpecifyKind(Now.ToOffset(TimeSpan.FromHours(7)).Date, DateTimeKind.Utc); // FIX B55
        var start = DateTime.SpecifyKind(request.StartDate.Date < today ? today : request.StartDate.Date, DateTimeKind.Utc);
        var membership = new Repository.Entity.Membership {
            Id = Guid.NewGuid(), MemberId = memberId, PackageId = package.Id,
            StartDate = start, EndDate = start.AddDays(package.DurationDays - 1), Status = MembershipStatus.Inactive
        };
        var invoice = new Repository.Entity.Invoice {
            Id = Guid.NewGuid(), Membership = membership, Amount = package.Price,
            // CODE CŨ: PaymentExpiresAt = Now.AddMinutes(30).
            ReceptionistId = null, PaymentDate = null, PaymentExpiresAt = Now.AddMinutes(15), OriginalAmount = package.Price, // FIX B53 B55
            Status = InvoiceStatus.Pending, PaymentMethod = (PaymentMethod)request.PaymentMethod
        };
        // FIX SEPAY: validate cấu hình và sinh QR TRƯỚC khi lưu invoice/thẻ, không tạo rác khi cấu hình sai.
        if (sepay.IsEnabled) await sepay.PrepareInvoiceAsync(invoice);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        if (tx != null) await tx.CommitAsync(); // FIX B55
        /* CODE CŨ B53: response membership riêng, không đồng bộ metadata mới.
        return new() { PaymentId = invoice.Id, InvoiceId = invoice.Id, MembershipId = membership.Id,
            // CODE CŨ: Amount = invoice.Amount, Status = 1, PaymentUrl = null
            // FIX SEPAY: PaymentUrl là URL ẢNH QR; FE dùng qrCodeUrl + GET trạng thái, không tin returnUrl.
            Amount = invoice.Amount, Status = 1, PaymentUrl = invoice.SePayQrCodeUrl, QrCodeUrl = invoice.SePayQrCodeUrl,
            PaymentReference = invoice.PaymentReference, ExpiresAt = invoice.PaymentExpiresAt,
            BankBin = invoice.SePayBankBin, BankAccountNumber = invoice.SePayAccountNumber };
        */
        return ClassCommerceService.CheckoutResponse(invoice); // FIX B53 B55
    }
    public async Task<Response.PaymentDetailResponse> GetPaymentAsync(Guid paymentId)
    {
        var row = await Load(paymentId);
        if (actor.IsMember)
        {
            // CODE CŨ: row.Membership.MemberId (không hỗ trợ lớp).
            if (PaymentQueries.MemberId(row) != await actor.MemberIdAsync()) // FIX B53
                throw new UnauthorizedAccessException("Bạn chỉ được xem giao dịch của chính mình.");
        }
        else if (!actor.CanConfirmPayment) throw new UnauthorizedAccessException("Không có quyền xem thanh toán.");
        // CODE CŨ: GET chỉ tính status=5 trong DTO, chờ worker 5 phút mới lưu invoice/thẻ.
        // FIX B59: sau kiểm tra quyền, đồng bộ chính đơn này trong một transaction trước response.
        await new PaymentExpirationService(db, clock ?? TimeProvider.System).ExpireAsync(paymentId: row.Id, asOf: Now); // FIX B59: cùng snapshot với DTO.
        return Map(row);
    }
    public async Task<Response.PaymentDetailResponse> ConfirmPaymentAsync(Guid paymentId)
    {
        // CODE CŨ: actor.CanConfirmPayment gồm Manager.
        if (!actor.IsReceptionist) throw new UnauthorizedAccessException("Chỉ lễ tân được xác nhận thu tiền thủ công."); // FIX B58
        await using var transaction = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var row = await Load(paymentId);
        // FIX B60: QR SePay chỉ nhận settlement qua webhook đã xác thực, không bypass bằng staff confirm.
        if (row.PaymentReference != null || row.SePayQrCodeUrl != null)
            throw new ConflictException("Đơn SePay chỉ được xác nhận qua webhook đối soát; không xác nhận thủ công bằng API này.");
        if (row.Status == InvoiceStatus.Paid) return Map(row); // idempotent, không đổi ngày trả.
        /* CODE CŨ:
        if (row.Status != InvoiceStatus.Pending || !row.PaymentExpiresAt.HasValue ||
            row.PaymentExpiresAt <= Now || row.Membership.IsDeleted || row.Membership.Status != MembershipStatus.Inactive)
            throw new ConflictException("Giao dịch đã hủy, hết hạn hoặc không còn chờ thanh toán.");
        if (row.Membership.Member.IsDeleted || row.Membership.Member.Account.IsDeleted || row.Membership.Member.Account.Status != AccountStatus.Active)
            throw new ConflictException("Tài khoản hội viên không còn hoạt động.");
        row.Status = InvoiceStatus.Paid; row.PaymentDate = Now; row.Membership.Status = MembershipStatus.Active;
        */
        // FIX SEPAY: chia sẻ quy tắc dữ liệu, vẫn giữ kiểm tra quyền staff ở đầu hàm này.
        var now = Now;
        var reason = await PaymentSettlement.RejectionReasonAsync(db, row, now); // FIX B55
        if (reason != null) throw new ConflictException(reason);
        // Xác nhận thủ công: chỉ ghi lễ tân nếu chính người gọi có profile lễ tân.
        // CODE CŨ: row.ReceptionistId = await ...
        var receptionistId = await db.Receptionists.Where(r => r.AccountId == actor.AccountId && !r.IsDeleted)
            .Select(r => (Guid?)r.Id).FirstOrDefaultAsync();
        PaymentSettlement.Apply(row, now, receptionistId, db); // FIX B53 B55 B57
        await db.SaveChangesAsync();
        if (transaction != null) await transaction.CommitAsync();
        return Map(row);
    }
    // CODE CŨ: Load chỉ Include Membership/Package/Member.
    private async Task<Repository.Entity.Invoice> Load(Guid id) => await PaymentQueries.Graph(db)
        .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted)
        ?? throw new KeyNotFoundException("Không tìm thấy giao dịch thanh toán.");
    private Response.PaymentDetailResponse Map(Repository.Entity.Invoice row) => new() {
        PaymentId = row.Id, InvoiceId = row.Id, MembershipId = row.MemberMembershipId,
        Amount = row.Amount, PackageName = row.Membership?.Package.PackageName ?? string.Empty, // FIX B53
        // CODE CŨ: hết hạn và hủy chung status 4, không có trạng thái đã hoàn.
        Status = row.Status == InvoiceStatus.Refunded ? 6 : row.Status == InvoiceStatus.Paid ? 2 :
            row.Status == InvoiceStatus.Cancelled ? (row.WasExpired ? 5 : 4) : row.PaymentExpiresAt <= Now ? 5 : 1, // FIX B56
        PaidAt = row.Status == InvoiceStatus.Paid ? row.PaymentDate : null,
        // FIX SEPAY: chỉ phát QR khi còn chờ; refresh dùng snapshot DB, không dựng lại theo ngân hàng mới.
        PaymentReference = row.PaymentReference, ExpiresAt = row.PaymentExpiresAt,
        QrCodeUrl = row.Status == InvoiceStatus.Pending && row.PaymentExpiresAt > Now && sepay.IsEnabled ? row.SePayQrCodeUrl : null,
        BankBin = row.SePayBankBin, BankAccountNumber = row.SePayAccountNumber,
        Kind = row.Kind, Provider = row.PaymentReference != null ? "SePay" : "Internal", PaymentMethod = (int)row.PaymentMethod,
        PayUrl = row.Status == InvoiceStatus.Pending && row.PaymentExpiresAt > Now ? row.SePayQrCodeUrl : null,
        BookingId = row.ClassBookingId, ClassId = row.ClassBooking?.ClassId, ClassName = row.ClassBooking?.Class.ClassName,
        PackageId = row.Membership?.PackageId, OriginalAmount = row.OriginalAmount, DiscountPercent = row.DiscountPercent,
        CreditAmount = row.CreditAmount, FromPackageName = row.FromPackageName,
        RefundAmount = row.ClassBooking?.RefundAmount, RefundPending = row.RefundRequestedAt != null && row.RefundedAt == null,
        RefundReference = row.RefundReference // FIX B53 B55 B56
    };
}
