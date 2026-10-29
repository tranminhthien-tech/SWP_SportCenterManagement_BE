using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;
namespace SWP_SportCenter.Service.Classes;
// FIX B56: status theo lịch, guard vẫn xét giờ thật để không phụ thuộc job kịp chạy.
public sealed class ClassLifecycleService(AppDbContext db, TimeProvider clock)
{
    public async Task<int> RefreshAsync(CancellationToken ct = default)
    {
        var rows = await db.Classes.Include(c => c.ClassSessions).Where(c => !c.IsDeleted &&
            (c.Status == ClassStatus.Scheduled || c.Status == ClassStatus.Ongoing)).ToListAsync(ct);
        int changed = 0; var now = clock.GetUtcNow(); var local = ClassRules.LocalNow(now);
        foreach (var c in rows) {
            var last = c.ClassSessions.Where(s => !s.IsDeleted).Select(s => (DateTime?)s.Date.ToDateTime(TimeOnly.MinValue).Add(s.EndTime)).Max();
            var status = last <= local ? ClassStatus.Completed : ClassRules.HasStarted(c, now) ? ClassStatus.Ongoing : c.Status;
            if (status != c.Status) { c.Status = status; changed++; }
        }
        if (changed > 0) await db.SaveChangesAsync(ct); return changed;
    }
}
