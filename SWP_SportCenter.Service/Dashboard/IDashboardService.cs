namespace SWP_SportCenter.Service.Dashboard;

public interface IDashboardService
{
    // GET: api/dashboard/revenue
    // Báo cáo doanh thu theo khoảng thời gian
    Task<Response.RevenueReportResponse> GetRevenueReportAsync(
        DateTimeOffset from,
        DateTimeOffset to);

    // GET: api/dashboard/members/summary
    // Thống kê thành viên (tổng, mới, active)
    Task<Response.MemberSummaryResponse> GetMemberSummaryAsync();

    // GET: api/dashboard/memberships/summary
    // Thống kê gói tập (đang hoạt động, sắp hết hạn)
    Task<Response.MembershipSummaryResponse> GetMembershipSummaryAsync();

    // GET: api/dashboard/classes/summary
    // Thống kê lớp học (tổng lớp, tổng buổi, tổng học viên đăng ký)
    Task<Response.ClassSummaryResponse> GetClassSummaryAsync();
}