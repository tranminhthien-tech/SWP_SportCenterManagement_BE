using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Membership;

// FIX B45 B47: dùng chung quy tắc thẻ theo NGÀY Việt Nam, không tự gia hạn hay kích hoạt.
public static class MembershipEligibility
{
    public static async Task<Repository.Entity.Membership?> FindAsync(AppDbContext db, Guid memberId, DateOnly day, Guid? categoryId = null)
        => await Query(db, memberId, day, categoryId).FirstOrDefaultAsync();

    // FIX B45 B47: query riêng để kiểm chứng SQL PostgreSQL mà không truy cập DB thật.
    public static IQueryable<Repository.Entity.Membership> Query(AppDbContext db, Guid memberId, DateOnly day, Guid? categoryId = null)
    {
        var date = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return db.Memberships.AsNoTracking().Include(m => m.Package)
            .Where(m => m.MemberId == memberId && !m.IsDeleted && m.Status == MembershipStatus.Active &&
                !m.Member.IsDeleted && !m.Member.Account.IsDeleted && m.Member.Account.Role == AccountRole.Member &&
                m.Member.Account.Status == AccountStatus.Active && m.StartDate.Date <= date && m.EndDate.Date >= date &&
                (!categoryId.HasValue || !m.Package.Categories.Any() || m.Package.Categories.Any(c => c.CategoryId == categoryId.Value)))
            .OrderByDescending(m => m.EndDate).ThenBy(m => m.Id);
    }
}
