using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Notification;

// FIX B32: nhắc một lần cho mỗi gói và ngày hết hạn; chạy lại không gửi trùng.
public sealed class MembershipExpiryNotifier(AppDbContext db, TimeProvider clock)
{
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var today = clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).Date;
        var min = DateTime.SpecifyKind(today, DateTimeKind.Utc);
        var max = min.AddDays(8); // gồm hôm nay đến hết ngày thứ 7
        var rows = await db.Memberships.AsNoTracking().Where(m => !m.IsDeleted &&
                m.Status == MembershipStatus.Active && m.EndDate >= min && m.EndDate < max &&
                !m.Member.IsDeleted && !m.Member.Account.IsDeleted && m.Member.Account.Status == AccountStatus.Active)
            .Select(m => new { m.Id, m.EndDate, m.Member.AccountId, m.Package.PackageName })
            .ToListAsync(cancellationToken);
        var count = 0;
        foreach (var row in rows)
        {
            var key = $"membership-expiry:{row.Id:N}:{row.EndDate:yyyyMMdd}";
            // Bao gồm cả thông báo đã xóa mềm để không gửi lại sau khi người dùng xóa.
            if (await db.Notifications.AnyAsync(n => n.DeduplicationKey == key, cancellationToken)) continue;
            db.Notifications.Add(new Repository.Entity.Notification {
                Id = Guid.NewGuid(), AccountId = row.AccountId, DeduplicationKey = key,
                Title = "Gói thành viên sắp hết hạn",
                Message = $"Gói {row.PackageName} của bạn hết hạn ngày {row.EndDate:dd/MM/yyyy}. Vui lòng liên hệ quầy để gia hạn."
            });
            count++;
        }
        if (count > 0) await db.SaveChangesAsync(cancellationToken);
        return count;
    }
}
