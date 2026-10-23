using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SWP_SportCenter.Repository;

namespace SWP_SportCenter.Service.Notification;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public NotificationService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    // Lấy AccountId từ JWT token
    private Guid GetCurrentAccountId()
    {
        var claim = _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(claim, out var id))
            throw new UnauthorizedAccessException("Không tìm thấy AccountId trong token.");
        return id;
    }

    public async Task<Base.Response.PageResult<Response.NotificationResponse>>
        GetMyNotificationsAsync(bool? isRead, int pageIndex, int pageSize)
    {
        var accountId = GetCurrentAccountId();

        var query = _db.Notifications
            .AsNoTracking()
            // CODE CŨ: .Where(n => n.AccountId == accountId);
            // FIX B32: thông báo đã xóa mềm không xuất hiện nhưng vẫn giữ khóa chống trùng.
            .Where(n => n.AccountId == accountId && !n.IsDeleted);

        if (isRead.HasValue)
            query = query.Where(n => n.IsRead == isRead.Value);

        var total = await query.CountAsync();

        var data = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new Response.NotificationResponse
            {
                Id = n.Id,
                AccountId = n.AccountId,
                Title = n.Title,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.NotificationResponse>
        {
            Data = data,
            TotalItems = total,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        var accountId = GetCurrentAccountId();

        var notification = await _db.Notifications
            // CODE CŨ: chỉ lọc Id và AccountId.
            // FIX B32: không thao tác thông báo đã xóa mềm.
            .FirstOrDefaultAsync(n => n.Id == id && n.AccountId == accountId && !n.IsDeleted);

        if (notification == null) return false;

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllAsReadAsync()
    {
        var accountId = GetCurrentAccountId();

        var unread = await _db.Notifications
            // CODE CŨ: .Where(n => n.AccountId == accountId && !n.IsRead)
            // FIX B32: chỉ tác động thông báo đang hiển thị.
            .Where(n => n.AccountId == accountId && !n.IsRead && !n.IsDeleted)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync();
        return unread.Count;
    }

    public async Task<Response.NotificationResponse> SendAsync(
        Request.SendNotificationRequest request)
    {
        if (request.AccountIds == null || request.AccountIds.Count == 0)
            throw new ArgumentException("Phải chọn ít nhất 1 người nhận.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Tiêu đề không được để trống.");

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Nội dung không được để trống.");

        // CODE CŨ: chỉ kiểm tra người nhận tồn tại, không giới hạn theo lớp của Coach.
        // FIX B29: không gửi một phần khi payload chứa bất kỳ người nhận ngoài phạm vi.
        if (_http.HttpContext?.User.IsInRole("Coach") == true)
        {
            var actorId = GetCurrentAccountId();
            var allowed = await _db.ClassBookings.Where(b => !b.IsDeleted &&
                    b.Status != Repository.Enum.BookingStatus.Cancelled && !b.Class.IsDeleted &&
                    !b.Class.Coach.IsDeleted && b.Class.Coach.AccountId == actorId &&
                    !b.Member.IsDeleted && !b.Member.Account.IsDeleted &&
                    b.Member.Account.Status == Repository.Enum.AccountStatus.Active)
                .Select(b => b.Member.AccountId).Distinct().ToListAsync();
            if (request.AccountIds.Distinct().Except(allowed).Any())
                throw new UnauthorizedAccessException("Coach chỉ được gửi thông báo tới học viên trong lớp mình phụ trách.");
        }
        else if (_http.HttpContext?.User.IsInRole("CenterManager") != true)
            throw new UnauthorizedAccessException("Bạn không có quyền gửi thông báo.");

        // Kiểm tra account tồn tại
        var existingIds = await _db.Accounts
            .Where(a => request.AccountIds.Contains(a.Id) && !a.IsDeleted)
            .Select(a => a.Id)
            .ToListAsync();

        var notifications = existingIds.Select(accountId =>
            new Repository.Entity.Notification
            {
                AccountId = accountId,
                Title = request.Title.Trim(),
                Message = request.Message.Trim(),
                IsRead = false
            }).ToList();

        // FIX B16: không First() danh sách rỗng, không lưu khi không có người nhận.
        if (notifications.Count == 0)
            throw new ArgumentException("Không có người nhận hợp lệ.");
        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();

    // Trả về thông báo đầu tiên làm đại diện
        var first = notifications.First();
        return new Response.NotificationResponse
        {
            Id = first.Id,
            // FIX B16: số người nhận hợp lệ sau khi lọc Account trùng/không tồn tại.
            SentCount = notifications.Count,
            AccountId = first.AccountId,
            Title = first.Title,
            Message = first.Message,
            IsRead = first.IsRead,
            CreatedAt = first.CreatedAt
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var accountId = GetCurrentAccountId();

        var notification = await _db.Notifications
            // CODE CŨ: chỉ lọc Id và AccountId.
            // FIX B32: xóa lại thông báo đã xóa trả NotFound.
            .FirstOrDefaultAsync(n => n.Id == id && n.AccountId == accountId && !n.IsDeleted);

        if (notification == null) return false;

        // CODE CŨ: _db.Notifications.Remove(notification);
        // FIX B32: giữ deduplication key để không gửi lại nhắc hạn người dùng đã xóa.
        notification.IsDeleted = true;
        await _db.SaveChangesAsync();
        return true;
    }
}
