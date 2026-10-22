using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.Notification;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationController(INotificationService service)
    {
        _service = service;
    }

    // GET: api/notifications/me
    // Lấy thông báo của user đang đăng nhập
    [HttpGet("me")]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] bool? isRead,
        // CODE CŨ: [FromQuery] int pageIndex = 1,
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 2147483647)] int pageIndex = 1,
        // CODE CŨ: [FromQuery] int pageSize = 10)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10)
    {
        var result = await _service.GetMyNotificationsAsync(isRead, pageIndex, pageSize);
        return Ok(result);
    }

    // PUT: api/notifications/{id}/read
    // Đánh dấu 1 thông báo đã đọc
    [HttpPut("{id:guid}/read")]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id)
    {
        var result = await _service.MarkAsReadAsync(id);

        if (!result)
            return NotFound("Không tìm thấy thông báo.");

        return Ok("Đã đánh dấu là đã đọc.");
    }

    // PUT: api/notifications/read-all
    // Đánh dấu tất cả thông báo đã đọc
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var count = await _service.MarkAllAsReadAsync();
        return Ok($"Đã đánh dấu {count} thông báo là đã đọc.");
    }

    // POST: api/notifications
    // Gửi thông báo (Manager)
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPost]
    public async Task<IActionResult> Send(
        [FromBody] Request.SendNotificationRequest request)
    {
        var result = await _service.SendAsync(request);
        return Ok(result);
    }

    // DELETE: api/notifications/{id}
    // Xóa thông báo
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
            return NotFound("Không tìm thấy thông báo.");

        return Ok("Xóa thông báo thành công.");
    }
}
