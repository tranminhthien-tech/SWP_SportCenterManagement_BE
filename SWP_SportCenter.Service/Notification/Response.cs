namespace SWP_SportCenter.Service.Notification;

public class Response
{
    public class NotificationResponse
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}