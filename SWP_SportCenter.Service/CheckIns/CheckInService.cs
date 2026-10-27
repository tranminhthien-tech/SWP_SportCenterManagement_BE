using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Service.Security;
using SWP_SportCenter.Service.Exceptions;

namespace SWP_SportCenter.Service.CheckIns;

public sealed class CreateCheckIn { public Guid MemberId { get; set; } }
public sealed class CheckInResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public Guid MembershipId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public DateTime MembershipEndDate { get; set; }
    public int DaysRemaining { get; set; }
    public Guid? ReceptionistId { get; set; }
    public Guid RecordedByAccountId { get; set; }
    public DateTimeOffset CheckInTime { get; set; }
}

// FIX B47: quyền vào trung tâm xét thẻ hôm nay, không xét môn và không ghi Attendance.
public sealed class CheckInService(AppDbContext db, CurrentActor actor, TimeProvider clock)
{
    private DateOnly Today => DateOnly.FromDateTime(clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7)).DateTime);
    public async Task<CheckInResponse> CreateAsync(CreateCheckIn request)
    {
        if (!actor.CanHandleSupport) throw new UnauthorizedAccessException("Chỉ lễ tân hoặc quản lý được check-in.");
        // FIX B47: chụp một thời điểm cho cả xét ngày và ghi lượt, tránh lệch khi qua nửa đêm.
        var now = clock.GetUtcNow();
        var day = DateOnly.FromDateTime(now.ToOffset(TimeSpan.FromHours(7)).DateTime);
        var membership = await Membership.MembershipEligibility.FindAsync(db, request.MemberId, day);
        if (membership == null) throw new BusinessException("Học viên chưa có thẻ thành viên còn hiệu lực hôm nay.");
        var receptionistId = await db.Receptionists.Where(r => r.AccountId == actor.AccountId && !r.IsDeleted)
            .Select(r => (Guid?)r.Id).FirstOrDefaultAsync();
        if (!actor.IsManager && receptionistId == null) throw new BusinessException("Tài khoản lễ tân chưa có hồ sơ hoạt động.");
        var row = new Repository.Entity.CheckIn { Id = Guid.NewGuid(), MemberId = request.MemberId,
            MembershipId = membership.Id, ReceptionistId = receptionistId, RecordedByAccountId = actor.AccountId,
            CheckInTime = now };
        db.Set<Repository.Entity.CheckIn>().Add(row);
        await db.SaveChangesAsync();
        return await Query().SingleAsync(x => x.Id == row.Id);
    }
    public async Task<Base.Response.PageResult<CheckInResponse>> GetAsync(DateOnly? date, int pageIndex, int pageSize)
    {
        if (!actor.IsMember && !actor.CanHandleSupport) throw new UnauthorizedAccessException("Không có quyền xem check-in.");
        if (pageIndex < 1 || pageSize < 1 || pageSize > 500 || (long)(pageIndex - 1) * pageSize > int.MaxValue)
            throw new ArgumentException("Phân trang không hợp lệ.");
        var query = Query();
        if (actor.IsMember) { var id = await actor.MemberIdAsync(); query = query.Where(x => x.MemberId == id); }
        if (date.HasValue) {
            var from = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
            var to = from.AddDays(1);
            query = query.Where(x => x.CheckInTime >= from && x.CheckInTime < to);
        }
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.CheckInTime).ThenBy(x => x.Id)
            .Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new() { Data = rows, TotalItems = total, PageIndex = pageIndex, PageSize = pageSize };
    }
    private IQueryable<CheckInResponse> Query()
    {
        var today = Today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        return db.Set<Repository.Entity.CheckIn>().AsNoTracking().Where(x => !x.IsDeleted)
            .Select(x => new CheckInResponse { Id = x.Id, MemberId = x.MemberId, MemberName = x.Member.FullName,
                MembershipId = x.MembershipId, PackageName = x.Membership.Package.PackageName,
                MembershipEndDate = x.Membership.EndDate,
                DaysRemaining = Math.Max(0, (x.Membership.EndDate.Date - today).Days),
                ReceptionistId = x.ReceptionistId, RecordedByAccountId = x.RecordedByAccountId, CheckInTime = x.CheckInTime });
    }
}
