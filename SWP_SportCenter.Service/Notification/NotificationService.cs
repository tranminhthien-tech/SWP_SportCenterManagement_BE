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
            .Where(n => n.AccountId == accountId);

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
            .FirstOrDefaultAsync(n => n.Id == id && n.AccountId == accountId);

        if (notification == null) return false;

        notification.IsRead = true;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> MarkAllAsReadAsync()
    {
        var accountId = GetCurrentAccountId();

        var unread = await _db.Notifications
            .Where(n => n.AccountId == accountId && !n.IsRead)
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

        _db.Notifications.AddRange(notifications);
        await _db.SaveChangesAsync();

    // Trả về thông báo đầu tiên làm đại diện
        var first = notifications.First();
        return new Response.NotificationResponse
        {
            Id = first.Id,
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
            .FirstOrDefaultAsync(n => n.Id == id && n.AccountId == accountId);

        if (notification == null) return false;

        _db.Notifications.Remove(notification);
        await _db.SaveChangesAsync();
        return true;
    }
}
