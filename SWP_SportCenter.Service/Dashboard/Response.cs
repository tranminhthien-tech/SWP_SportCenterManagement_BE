namespace SWP_SportCenter.Service.Dashboard;

public class Response
{
    // Báo cáo doanh thu
    public class RevenueReportResponse
    {
        public DateTimeOffset From { get; set; }
        public DateTimeOffset To { get; set; }

        // Tổng doanh thu trong kỳ
        public decimal TotalRevenue { get; set; }

        // Số hóa đơn đã thanh toán
        public int PaidInvoiceCount { get; set; }

        // Doanh thu theo từng tháng (dùng cho chart)
        public List<MonthlyRevenueItem> MonthlyRevenue { get; set; } = new();
    }

    public class MonthlyRevenueItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public int InvoiceCount { get; set; }
    }

    // Thống kê thành viên
    public class MemberSummaryResponse
    {
        // Tổng số member (không bị xóa)
        public int TotalMembers { get; set; }

        // Member mới trong 30 ngày gần nhất
        public int NewMembersLast30Days { get; set; }

        // Member có gói tập đang Active
        public int ActiveMembers { get; set; }
    }

    // Thống kê gói tập
    public class MembershipSummaryResponse
    {
        // Tổng gói đang hoạt động
        public int ActiveMemberships { get; set; }

        // Gói sắp hết hạn trong 7 ngày tới
        public int ExpiringSoon { get; set; }

        // Gói đã hết hạn
        public int ExpiredMemberships { get; set; }

        // Gói Inactive (không hoạt động)
        public int InactiveMemberships { get; set; }
    }

    // Thống kê lớp học
    public class ClassSummaryResponse
    {
        // Tổng số lớp
        public int TotalClasses { get; set; }

        // Tổng số buổi học
        public int TotalSessions { get; set; }

        // Tổng lượt đăng ký lớp (ClassBooking)
        public int TotalBookings { get; set; }
    }
}