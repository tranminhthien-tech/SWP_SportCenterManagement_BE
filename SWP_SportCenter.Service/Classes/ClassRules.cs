using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Exceptions;
namespace SWP_SportCenter.Service.Classes;

// FIX B56 B58: lịch lớp dùng UTC+7, không theo timezone Render.
public static class ClassRules
{
    public static DateTime LocalNow(DateTimeOffset now) => now.ToOffset(TimeSpan.FromHours(7)).DateTime;
    public static DateTime? FirstStart(Repository.Entity.Class c) => c.ClassSessions.Where(s => !s.IsDeleted)
        .Select(s => (DateTime?)s.Date.ToDateTime(TimeOnly.MinValue).Add(s.StartTime)).Min();
    public static bool HasStarted(Repository.Entity.Class c, DateTimeOffset now) => c.Status == ClassStatus.Ongoing || FirstStart(c) <= LocalNow(now);
    public static bool IsRunning(Repository.Entity.Class c, DateTimeOffset now)
    {
        if (c.Status is ClassStatus.Completed or ClassStatus.Cancelled) return false;
        var last = c.ClassSessions.Where(s => !s.IsDeleted).Select(s => (DateTime?)s.Date.ToDateTime(TimeOnly.MinValue).Add(s.EndTime)).Max();
        return (c.Status == ClassStatus.Ongoing || FirstStart(c) <= LocalNow(now)) && (!last.HasValue || last > LocalNow(now));
    }
    public static void EnsureBookable(Repository.Entity.Class c, DateTimeOffset now)
    {
        if (HasStarted(c, now)) throw new BusinessException("Lớp đã bắt đầu, không nhận đặt thêm.");
        if (c.IsDeleted || c.Status != ClassStatus.Scheduled) throw new BusinessException("Lớp không còn nhận đăng ký.");
        if (!FirstStart(c).HasValue) throw new BusinessException("Lớp học chưa có lịch, không thể đăng ký.");
    }
}
