using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Security;
namespace SWP_SportCenter.Service.Ai;

// FIX B48 01/11: context chỉ đọc, có giới hạn, không lấy ID lịch/người khác từ body hoặc history.
public sealed class AiContextBuilder(AppDbContext db, CurrentActor actor, TimeProvider clock)
{
    public async Task<object> BuildAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var day = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var categories = await db.SportCategories.AsNoTracking().Where(c => !c.IsDeleted)
            .OrderBy(c => c.CategoryName).Take(50).Select(c => c.CategoryName).ToListAsync(ct);
        var classes = await db.Classes.AsNoTracking().Where(c => !c.IsDeleted &&
            (c.Status == ClassStatus.Scheduled || c.Status == ClassStatus.Ongoing))
            .OrderBy(c => c.StartDate).ThenBy(c => c.Id).Take(20)
            .Select(c => new { c.ClassName, Category = c.Category.CategoryName, c.Price, c.StartDate, c.EndDate, c.Status }).ToListAsync(ct);
        var publicSessions = await db.ClassSessions.AsNoTracking().Where(s => !s.IsDeleted && !s.Class.IsDeleted &&
            s.Date >= day && (s.Class.Status == ClassStatus.Scheduled || s.Class.Status == ClassStatus.Ongoing))
            .OrderBy(s => s.Date).ThenBy(s => s.StartTime).Take(20)
            .Select(s => new { s.Class.ClassName, s.Date, s.StartTime, s.EndTime }).ToListAsync(ct);
        // Không cấp danh sách riêng của toàn trung tâm cho Receptionist/Manager qua AI.
        var ownSessions = db.ClassSessions.AsNoTracking().Where(s => !s.IsDeleted && !s.Class.IsDeleted &&
            s.Date >= day && (s.Class.Status == ClassStatus.Scheduled || s.Class.Status == ClassStatus.Ongoing));
        if (actor.IsMember) {
            var id = await actor.MemberIdAsync();
            ownSessions = ownSessions.Where(s => db.ClassBookings.Any(b => b.ClassId == s.ClassId && b.MemberId == id &&
                !b.IsDeleted && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Attended)));
        } else if (actor.IsCoach) {
            var accountId = actor.AccountId;
            ownSessions = ownSessions.Where(s => !s.Class.Coach.IsDeleted && s.Class.Coach.AccountId == accountId);
        } else ownSessions = ownSessions.Where(s => false);
        var personalSchedule = await ownSessions.OrderBy(s => s.Date).ThenBy(s => s.StartTime).Take(20)
            .Select(s => new { s.Class.ClassName, s.Date, s.StartTime, s.EndTime }).ToListAsync(ct);
        return new { AsOfUtc = now, TimeZone = "Asia/Ho_Chi_Minh", Categories = categories, Classes = classes,
            PublicSessions = publicSessions, PersonalSchedule = personalSchedule,
            Limits = new { Categories = 50, Classes = 20, PublicSessions = 20, PersonalSchedule = 20 } };
    }
}
