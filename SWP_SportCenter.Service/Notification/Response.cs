namespace SWP_SportCenter.Service.Notification;

public class Response
{
    public class NotificationResponse
    {
        public Guid? SenderAccountId { get; set; } // FIX B57
        public string SenderName { get; set; } = "Hệ thống";
        public string? SenderRole { get; set; }
        // FIX B16: endpoint gửi thông báo trả số người nhận thực tế.
        public int? SentCount { get; set; }
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
