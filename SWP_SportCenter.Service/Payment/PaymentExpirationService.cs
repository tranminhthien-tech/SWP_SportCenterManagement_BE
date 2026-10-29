using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Payment;

// FIX B39: giữ lịch sử, hủy checkout quá hạn thay vì xóa tài liệu tài chính.
public sealed class PaymentExpirationService(AppDbContext db, TimeProvider clock)
{
    public async Task<int> ExpireAsync(CancellationToken ct = default)
    {
        await using var tx = db.Database.IsRelational()
            ? await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct) : null;
        var now = clock.GetUtcNow();
        // CODE CŨ: chỉ Include Membership.
        var rows = await db.Invoices.Include(i => i.Membership).Include(i => i.ClassBooking).Where(i => !i.IsDeleted &&
            i.Status == InvoiceStatus.Pending && i.PaymentExpiresAt != null && i.PaymentExpiresAt <= now)
            .OrderBy(i => i.PaymentExpiresAt).Take(200).ToListAsync(ct);
        foreach (var row in rows) {
            row.Status = InvoiceStatus.Cancelled;
            row.WasExpired = true; // FIX B53 B55: không nhầm user hủy với hết hạn.
            if (row.Membership?.Status == MembershipStatus.Inactive) row.Membership.Status = MembershipStatus.Cancelled;
            if (row.ClassBooking?.Status == BookingStatus.PendingPayment) { row.ClassBooking.Status = BookingStatus.Cancelled; row.ClassBooking.CancelledAt = now; }
        }
        if (rows.Count > 0) await db.SaveChangesAsync(ct);
        if (tx != null) await tx.CommitAsync(ct);
        return rows.Count;
    }
}
