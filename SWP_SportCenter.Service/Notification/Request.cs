namespace SWP_SportCenter.Service.Notification;

public class Request
{
    public class SendNotificationRequest
    {
        // Danh sách AccountId nhận thông báo
        public List<Guid> AccountIds { get; set; } = new();

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;
    }
}