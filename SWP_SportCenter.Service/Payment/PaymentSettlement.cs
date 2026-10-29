/* CODE CŨ B53 B55: settlement chỉ hỗ trợ Membership.
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Payment;

// FIX SEPAY: dùng chung điều kiện dữ liệu cho webhook và xác nhận nội bộ, không giả mạo role staff.
public static class PaymentSettlement
{
    public static string? RejectionReason(Repository.Entity.Invoice row, DateTimeOffset now)
    {
        if (row.IsDeleted || row.Status != InvoiceStatus.Pending || !row.PaymentExpiresAt.HasValue ||
            row.PaymentExpiresAt <= now || row.Membership.IsDeleted || row.Membership.Status != MembershipStatus.Inactive)
            return "Giao dịch đã hủy, hết hạn hoặc không còn chờ thanh toán.";
        if (row.Membership.Member.IsDeleted || row.Membership.Member.Account.IsDeleted ||
            row.Membership.Member.Account.Status != AccountStatus.Active || row.Membership.Member.Account.Role != AccountRole.Member)
            return "Tài khoản hội viên không còn hoạt động.";
        return null;
    }
    public static void Apply(Repository.Entity.Invoice row, DateTimeOffset now, Guid? receptionistId)
    {
        row.Status = InvoiceStatus.Paid;
        row.PaymentDate = now;
        row.Membership.Status = MembershipStatus.Active;
        row.ReceptionistId = receptionistId;
    }
}
*/
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Classes;
namespace SWP_SportCenter.Service.Payment;

// FIX B53 B55: mỗi kind áp dụng đúng đối tượng, không kích hoạt thẻ giảm giá khi mua lớp.
public static class PaymentSettlement
{
    // FIX B55: lễ tân có thể gia hạn trong lúc đơn nâng cấp đang chờ, kiểm tra lại trước khi kích hoạt.
    public static async Task<string?> RejectionReasonAsync(AppDbContext db, Repository.Entity.Invoice row, DateTimeOffset now)
    {
        var reason = RejectionReason(row, now);
        if (reason != null) return reason;
        if (row.Kind == "Upgrade" && await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(db.Memberships,
            m => m.MemberId == row.Membership!.MemberId && m.Id != row.UpgradeFromMembershipId && !m.IsDeleted &&
                m.Status == MembershipStatus.Active && m.StartDate > row.UpgradeFromMembership!.StartDate))
            return "Đã có kỳ gia hạn khác sau khi tạo báo giá nâng cấp, cần đối soát.";
        return null;
    }
    public static string? RejectionReason(Repository.Entity.Invoice row, DateTimeOffset now)
    {
        if (row.IsDeleted || row.Status != InvoiceStatus.Pending || row.PaymentExpiresAt <= now || !row.PaymentExpiresAt.HasValue)
            return "Giao dịch đã hủy, hết hạn hoặc không còn chờ thanh toán.";
        var member = row.ClassBooking?.Member ?? row.Membership?.Member;
        if (member == null || member.IsDeleted || member.Account.IsDeleted || member.Account.Status != AccountStatus.Active || member.Account.Role != AccountRole.Member)
            return "Tài khoản hội viên không còn hoạt động.";
        var today = DateTime.SpecifyKind(now.ToOffset(TimeSpan.FromHours(7)).Date, DateTimeKind.Utc);
        if (row.Kind == "Class") {
            var b = row.ClassBooking;
            if (b == null || b.IsDeleted || b.Status != BookingStatus.PendingPayment || b.Class.IsDeleted || b.Class.Status != ClassStatus.Scheduled || ClassRules.HasStarted(b.Class, now))
                return "Lượt đặt không còn chờ thanh toán hoặc lớp đã bắt đầu.";
            if (b.DiscountPercent > 0 && (b.Membership == null || b.Membership.IsDeleted || b.Membership.Status != MembershipStatus.Active || b.Membership.EndDate.Date < today || b.Membership.StartDate.Date > today))
                return "Thẻ giảm học phí không còn hiệu lực tại ngày thanh toán.";
        } else {
            if (row.Membership == null || row.Membership.IsDeleted || row.Membership.Status != MembershipStatus.Inactive) return "Thẻ không còn chờ kích hoạt.";
            if (row.Kind == "Upgrade" && (row.UpgradeFromMembership == null || row.UpgradeFromMembership.IsDeleted || row.UpgradeFromMembership.Status != MembershipStatus.Active ||
                row.UpgradeFromMembership.EndDate != row.UpgradeSourceEndDate || row.Membership.StartDate.Date != today))
                return "Thẻ gốc đã thay đổi hoặc báo giá nâng cấp qua ngày, cần đối soát.";
        }
        return null;
    }
    public static void Apply(Repository.Entity.Invoice row, DateTimeOffset now, Guid? receptionistId, AppDbContext? db = null)
    {
        row.Status = InvoiceStatus.Paid; row.PaymentDate = now; row.ReceptionistId = receptionistId;
        if (row.Kind == "Class") { row.ClassBooking!.Status = BookingStatus.Booked; row.ClassBooking.PaymentExpiresAt = null; }
        else {
            row.Membership!.Status = MembershipStatus.Active;
            if (row.Kind == "Upgrade") {
                row.UpgradeFromMembership!.EndDate = DateTime.SpecifyKind(now.ToOffset(TimeSpan.FromHours(7)).Date, DateTimeKind.Utc);
                row.UpgradeFromMembership.Status = MembershipStatus.Expired;
            }
        }
        var member = row.ClassBooking?.Member ?? row.Membership?.Member;
        if (db != null && member != null) db.Notifications.Add(new Repository.Entity.Notification {
            Id = Guid.NewGuid(), AccountId = member.AccountId, Title = "Thanh toán thành công",
            Message = $"Đã xác nhận thanh toán hóa đơn {row.Id}. Số tiền {row.Amount:0} VND.",
            DeduplicationKey = $"payment-paid:{row.Id:N}", SenderAccountId = null
        });
    }
}
