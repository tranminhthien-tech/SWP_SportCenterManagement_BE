using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Classes;
using SWP_SportCenter.Service.Exceptions;
using SWP_SportCenter.Service.Security;

namespace SWP_SportCenter.Service.Payment;

// FIX B53 B55 B56: tính tiền ở BE, một transaction cho hold/invoice, không tích hợp MoMo.
public sealed class ClassCommerceService(AppDbContext db, CurrentActor actor, Sepay.ISepayService sepay, TimeProvider clock)
{
    private DateTimeOffset? _now;
    public DateTimeOffset Now => _now ??= clock.GetUtcNow(); // FIX: cùng mốc báo giá/ngày/TTL trong một request scope.
    private DateTime Today => DateTime.SpecifyKind(Now.ToOffset(TimeSpan.FromHours(7)).Date, DateTimeKind.Utc);
    public async Task EnsureNoPackagePending(Guid memberId)
    {
        var old = await db.Invoices.Where(i => !i.IsDeleted && i.Status == InvoiceStatus.Pending && i.PaymentExpiresAt > Now &&
            i.Membership != null && i.Membership.MemberId == memberId).Select(i => (Guid?)i.Id).FirstOrDefaultAsync();
        if (old.HasValue) throw new PendingPaymentException(old.Value);
    }
    public async Task<Repository.Entity.Invoice> BuildClassAsync(Guid memberId, Guid classId, int method, bool counter)
    {
        var cls = await db.Classes.Include(c => c.ClassSessions).SingleOrDefaultAsync(c => c.Id == classId && !c.IsDeleted)
            ?? throw new KeyNotFoundException("Lớp không tồn tại.");
        ClassRules.EnsureBookable(cls, Now);
        if (!await db.Members.AnyAsync(m => m.Id == memberId && !m.IsDeleted && !m.Account.IsDeleted && m.Account.Role == AccountRole.Member && m.Account.Status == AccountStatus.Active))
            throw new BusinessException("Học viên không hoạt động.");
        var duplicate = await db.ClassBookings.Include(b => b.Invoice).FirstOrDefaultAsync(b => b.MemberId == memberId && b.ClassId == classId && !b.IsDeleted && b.Status != BookingStatus.Cancelled &&
            (b.Status != BookingStatus.PendingPayment || b.PaymentExpiresAt > Now));
        if (duplicate != null) {
            if (duplicate.Status == BookingStatus.PendingPayment && duplicate.Invoice != null) throw new PendingPaymentException(duplicate.Invoice.Id);
            throw new ConflictException("Học viên này đã đăng ký lớp học này rồi.");
        }
        // FIX B56: dọn hold hết hạn cùng transaction trước khi rebook, không mất lịch sử.
        var expired = await db.ClassBookings.Include(b => b.Invoice).Where(b => b.MemberId == memberId && b.ClassId == classId &&
            b.Status == BookingStatus.PendingPayment && b.PaymentExpiresAt <= Now).ToListAsync();
        foreach (var b in expired) { b.Status = BookingStatus.Cancelled; b.CancelledAt = Now; if (b.Invoice != null) { b.Invoice.Status = InvoiceStatus.Cancelled; b.Invoice.WasExpired = true; } }
        // FIX B56: giải phóng unique filtered key trước INSERT lượt mới; vẫn trong transaction bên ngoài.
        if (expired.Count > 0) await db.SaveChangesAsync();
        var occupied = await db.ClassBookings.CountAsync(b => b.ClassId == classId && !b.IsDeleted && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended ||
            (b.Status == BookingStatus.PendingPayment && b.PaymentExpiresAt > Now)));
        if (occupied >= cls.MaxCapacity) throw new ConflictException("Lớp học đã đạt số lượng học viên tối đa.");
        // Thẻ chỉ giảm học phí, không phải điều kiện được học; gói ngừng bán vẫn giữ quyền thẻ đã mua.
        var card = await db.Memberships.Include(m => m.Package).Where(m => m.MemberId == memberId && !m.IsDeleted && m.Status == MembershipStatus.Active &&
            m.StartDate.Date <= Today && m.EndDate.Date >= Today).OrderByDescending(m => m.Package.DiscountPercent).ThenByDescending(m => m.EndDate).FirstOrDefaultAsync();
        var discount = card?.Package.DiscountPercent ?? 0;
        var price = decimal.Floor(cls.Price * (100 - discount) / 100m);
        var booking = new Repository.Entity.ClassBooking { Id = Guid.NewGuid(), MemberId = memberId, Class = cls,
            OriginalPrice = cls.Price, DiscountPercent = discount, Price = price, MembershipId = discount > 0 ? card?.Id : null,
            Status = counter || price == 0 ? BookingStatus.Booked : BookingStatus.PendingPayment, BookingDate = Now,
            PaymentExpiresAt = counter || price == 0 ? null : Now.AddMinutes(15) };
        var invoice = new Repository.Entity.Invoice { Id = Guid.NewGuid(), ClassBooking = booking, Kind = "Class", OriginalAmount = cls.Price,
            DiscountPercent = discount, Amount = price, PaymentMethod = (PaymentMethod)method,
            Status = counter || price == 0 ? InvoiceStatus.Paid : InvoiceStatus.Pending,
            PaymentDate = counter || price == 0 ? Now : null, PaymentExpiresAt = booking.PaymentExpiresAt };
        if (counter) {
            if (!actor.IsReceptionist || method is < 1 or > 3) throw new UnauthorizedAccessException("Chỉ lễ tân được thu học phí tại quầy.");
            invoice.ReceptionistId = await db.Receptionists.Where(r => r.AccountId == actor.AccountId && !r.IsDeleted).Select(r => (Guid?)r.Id).SingleOrDefaultAsync()
                ?? throw new BusinessException("Tài khoản lễ tân chưa có hồ sơ.");
        } else if (price > 0) {
            if (method != 2 || !sepay.IsEnabled) throw new BusinessException("Thanh toán lớp yêu cầu SePay chuyển khoản đã cấu hình.");
            await sepay.PrepareInvoiceAsync(invoice);
        }
        db.Invoices.Add(invoice);
        return invoice;
    }
    public async Task<Response.CheckoutResponse> ClassCheckoutAsync(Request.ClassCheckoutRequest request)
    {
        if (!actor.IsMember) throw new UnauthorizedAccessException("Chỉ Member được mua lớp online.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var invoice = await BuildClassAsync(await actor.MemberIdAsync(), request.ClassId, request.PaymentMethod, false);
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return CheckoutResponse(invoice);
    }
    public async Task<Response.CheckoutResponse> UpgradeAsync(Request.UpgradeCheckoutRequest request)
    {
        if (!actor.IsMember) throw new UnauthorizedAccessException("Chỉ Member được nâng cấp thẻ của mình.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var memberId = await actor.MemberIdAsync(); await EnsureNoPackagePending(memberId);
        var source = await db.Memberships.Include(m => m.Package).Include(m => m.Invoice).Include(m => m.Member).ThenInclude(m => m.Account)
            .SingleOrDefaultAsync(m => m.Id == request.MembershipId && m.MemberId == memberId && !m.IsDeleted)
            ?? throw new KeyNotFoundException("Không tìm thấy thẻ của bạn.");
        if (source.Status != MembershipStatus.Active || source.StartDate.Date > Today || source.EndDate.Date < Today) throw new BusinessException("Thẻ phải đang hoạt động và còn hiệu lực hôm nay.");
        if (source.Invoice?.Status != InvoiceStatus.Paid) throw new BusinessException("Thẻ chưa có hóa đơn đã thanh toán để tính tiền còn lại.");
        if (await db.Memberships.AnyAsync(m => m.MemberId == memberId && m.Id != source.Id && !m.IsDeleted && m.Status == MembershipStatus.Active && m.StartDate > source.StartDate))
            throw new ConflictException("Đã có thẻ gia hạn bắt đầu sau thẻ này, không thể nâng cấp.");
        var target = await db.MembershipPackages.SingleOrDefaultAsync(p => p.Id == request.MembershipPackageId && !p.IsDeleted && p.Status == MembershipStatus.Active)
            ?? throw new BusinessException("Gói mới không đang bán.");
        if (target.DurationDays <= source.Package.DurationDays || target.Price <= source.Package.Price) throw new BusinessException("Chỉ được nâng cấp lên gói dài hơn và đắt hơn.");
        var totalDays = Math.Max(1, (source.EndDate.Date - source.StartDate.Date).Days + 1);
        var remaining = Math.Max(0, (source.EndDate.Date - Today).Days); // không tính ngày hôm nay.
        var credit = decimal.Floor(source.Invoice.Amount * remaining / totalDays / 1000m) * 1000m;
        var amount = Math.Max(0, target.Price - credit);
        var newCard = new Repository.Entity.Membership { Id = Guid.NewGuid(), MemberId = memberId, Member = source.Member, Package = target,
            StartDate = Today, EndDate = Today.AddDays(target.DurationDays - 1), Status = MembershipStatus.Inactive, UpgradedFromId = source.Id };
        var invoice = new Repository.Entity.Invoice { Id = Guid.NewGuid(), Membership = newCard, Kind = "Upgrade", OriginalAmount = target.Price,
            CreditAmount = credit, Amount = amount, Status = InvoiceStatus.Pending, PaymentMethod = PaymentMethod.BankTransfer,
            PaymentExpiresAt = Now.AddMinutes(15), UpgradeFromMembership = source, UpgradeSourceEndDate = source.EndDate, FromPackageName = source.Package.PackageName };
        if (request.PaymentMethod != 2) throw new BusinessException("Nâng cấp online chỉ hỗ trợ chuyển khoản SePay.");
        if (amount > 0) { if (!sepay.IsEnabled) throw new BusinessException("SePay chưa được cấu hình."); await sepay.PrepareInvoiceAsync(invoice); }
        else PaymentSettlement.Apply(invoice, Now, null, db);
        db.Invoices.Add(invoice); await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync(); return CheckoutResponse(invoice);
    }
    public async Task CancelPaymentAsync(Guid paymentId)
    {
        if (!actor.IsMember) throw new UnauthorizedAccessException("Chỉ chủ đơn Member được hủy đơn online.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var row = await PaymentQueries.Graph(db).SingleOrDefaultAsync(i => i.Id == paymentId && !i.IsDeleted) ?? throw new KeyNotFoundException("Không có đơn.");
        if (PaymentQueries.MemberId(row) != await actor.MemberIdAsync()) throw new UnauthorizedAccessException("Chỉ được hủy đơn của mình.");
        if (row.Status != InvoiceStatus.Pending) throw new ConflictException("Chỉ được hủy đơn đang chờ.");
        row.Status = InvoiceStatus.Cancelled;
        if (row.ClassBooking != null) { row.ClassBooking.Status = BookingStatus.Cancelled; row.ClassBooking.CancelledAt = Now; }
        if (row.Membership?.Status == MembershipStatus.Inactive) row.Membership.Status = MembershipStatus.Cancelled;
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync();
    }
    public async Task<ClassBooking.Response.CancellationResponse?> CancelBookingAsync(Guid id, Guid memberId)
    {
        if (actor.IsMember) memberId = await actor.MemberIdAsync();
        else if (!actor.IsReceptionist) throw new UnauthorizedAccessException("Chỉ Member hoặc lễ tân được hủy lượt đặt.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var booking = await db.ClassBookings.Include(b => b.Class).ThenInclude(c => c.ClassSessions).Include(b => b.Invoice)
            .SingleOrDefaultAsync(b => b.Id == id && b.MemberId == memberId && !b.IsDeleted);
        if (booking == null) return null;
        var invoice = booking.Invoice;
        if (booking.Status != BookingStatus.Cancelled) {
            if (invoice?.Status == InvoiceStatus.Pending) invoice.Status = InvoiceStatus.Cancelled;
            // FIX B56: chưa chuyển tiền thực tế thì chỉ yêu cầu hoàn, tuyệt đối không giả đã hoàn SePay.
            if (invoice?.Status == InvoiceStatus.Paid && invoice.Amount > 0 && !ClassRules.HasStarted(booking.Class, Now)) invoice.RefundRequestedAt ??= Now;
            booking.Status = BookingStatus.Cancelled; booking.CancelledAt = Now;
            await db.SaveChangesAsync();
        }
        if (tx != null) await tx.CommitAsync();
        return new() { BookingId = booking.Id, Status = booking.Status, Refunded = invoice?.Status == InvoiceStatus.Refunded,
            RefundAmount = booking.RefundAmount ?? 0, RefundPending = invoice?.RefundRequestedAt != null && invoice.RefundedAt == null,
            RequestedRefundAmount = invoice?.RefundRequestedAt != null ? invoice.Amount : 0 };
    }
    public async Task ConfirmRefundAsync(Guid id, Request.ConfirmRefundRequest request)
    {
        if (!actor.IsReceptionist) throw new UnauthorizedAccessException("Chỉ lễ tân được xác nhận đã hoàn tiền.");
        if (string.IsNullOrWhiteSpace(request.Reference) || request.Reference.Length > 200) throw new ArgumentException("Phải có mã đối soát chuyển trả hoặc biên nhận tiền mặt.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
        var row = await PaymentQueries.Graph(db).SingleOrDefaultAsync(i => i.Id == id && !i.IsDeleted) ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");
        if (row.Status == InvoiceStatus.Refunded) return;
        if (row.Status != InvoiceStatus.Paid || row.RefundRequestedAt == null || row.ClassBooking?.Status != BookingStatus.Cancelled)
            throw new ConflictException("Hóa đơn không có yêu cầu hoàn đủ điều kiện.");
        row.Status = InvoiceStatus.Refunded; row.RefundedAt = Now; row.RefundConfirmedByAccountId = actor.AccountId; row.RefundReference = request.Reference.Trim();
        row.ClassBooking.RefundAmount = row.Amount;
        db.Notifications.Add(new Repository.Entity.Notification { Id = Guid.NewGuid(), AccountId = row.ClassBooking.Member.AccountId,
            Title = "Đã xác nhận hoàn tiền", Message = $"Hóa đơn {row.Id} đã được lễ tân xác nhận hoàn {row.Amount:0} VND. Mã đối soát: {row.RefundReference}." });
        await db.SaveChangesAsync(); if (tx != null) await tx.CommitAsync();
    }
    public static Response.CheckoutResponse CheckoutResponse(Repository.Entity.Invoice i) => new() {
        PaymentId = i.Id, InvoiceId = i.Id, MembershipId = i.MemberMembershipId ?? i.Membership?.Id, Kind = i.Kind,
        BookingId = i.ClassBookingId ?? i.ClassBooking?.Id, Amount = i.Amount, OriginalAmount = i.OriginalAmount, DiscountPercent = i.DiscountPercent,
        CreditAmount = i.CreditAmount, FromPackageName = i.FromPackageName, Status = i.Status == InvoiceStatus.Paid ? 2 : 1, Provider = i.PaymentReference != null ? "SePay" : "Internal",
        PaymentMethod = (int)i.PaymentMethod, PaymentUrl = i.SePayQrCodeUrl, PayUrl = i.SePayQrCodeUrl, QrCodeUrl = i.SePayQrCodeUrl,
        PaymentReference = i.PaymentReference, ExpiresAt = i.PaymentExpiresAt, BankBin = i.SePayBankBin, BankAccountNumber = i.SePayAccountNumber
    };
}
