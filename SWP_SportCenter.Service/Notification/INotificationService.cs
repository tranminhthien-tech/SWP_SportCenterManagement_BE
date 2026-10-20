namespace SWP_SportCenter.Service.Notification;

public interface INotificationService
{
    // GET: api/notifications/me
    // Lấy danh sách thông báo của user đang đăng nhập
    Task<Base.Response.PageResult<Response.NotificationResponse>>
        GetMyNotificationsAsync(bool? isRead, int pageIndex, int pageSize);

    // PUT: api/notifications/{id}/read
    // Đánh dấu 1 thông báo đã đọc
    Task<bool> MarkAsReadAsync(Guid id);

    // PUT: api/notifications/read-all
    // Đánh dấu tất cả thông báo đã đọc
    Task<int> MarkAllAsReadAsync();

    // POST: api/notifications  (dành cho Manager gửi thông báo)
    // Gửi thông báo đến 1 hoặc nhiều account
    Task<Response.NotificationResponse> SendAsync(Request.SendNotificationRequest request);

    // DELETE: api/notifications/{id}
    // Xóa thông báo
    Task<bool> DeleteAsync(Guid id);
}