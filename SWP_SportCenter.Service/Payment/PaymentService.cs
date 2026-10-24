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
