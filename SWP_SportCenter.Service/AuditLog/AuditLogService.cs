using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.AuditLog;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly Security.CurrentActor _actor;

    // CODE CŨ: AuditLogService(AppDbContext context).
    // FIX B33: token là nguồn duy nhất của danh tính người ghi.
    public AuditLogService(AppDbContext context, Security.CurrentActor actor)
    {
        _context = context;
        _actor = actor;
    }

    public async Task<IEnumerable<Response.AuditLogResponse>> GetAllAsync()
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.Account)
            .OrderByDescending(a => a.Timestamp) // Log mới nhất lên đầu
            .Select(a => new Response.AuditLogResponse
            {
                Id = a.Id,
                AccountId = a.AccountId,
                Username = a.Account != null ? a.Account.Username : string.Empty,
                ActionType = a.ActionType,
                Description = a.Description,
                Timestamp = a.Timestamp
            })
            .ToListAsync();
    }

    public async Task<Response.AuditLogResponse?> GetByIdAsync(Guid id)
    {
        var log = await _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.Account)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (log == null) return null;

        return new Response.AuditLogResponse
        {
            Id = log.Id,
            AccountId = log.AccountId,
            Username = log.Account != null ? log.Account.Username : string.Empty,
            ActionType = log.ActionType,
            Description = log.Description,
            Timestamp = log.Timestamp
        };
    }

    public async Task<IEnumerable<Response.AuditLogResponse>> GetByAccountIdAsync(Guid accountId)
    {
        return await _context.AuditLogs
            .AsNoTracking()
            .Include(a => a.Account)
            .Where(a => a.AccountId == accountId)
            .OrderByDescending(a => a.Timestamp)
            .Select(a => new Response.AuditLogResponse
            {
                Id = a.Id,
                AccountId = a.AccountId,
                Username = a.Account != null ? a.Account.Username : string.Empty,
                ActionType = a.ActionType,
                Description = a.Description,
                Timestamp = a.Timestamp
            })
            .ToListAsync();
    }

    public async Task<Response.AuditLogResponse> LogAsync(Request.CreateAuditLogRequest request)
    {
        var accountId = _actor.AccountId; // FIX B33
        // CODE CŨ: kiểm tra và lưu request.AccountId do FE cung cấp.
        var isAccountExist = await _context.Accounts.AnyAsync(a => a.Id == accountId && !a.IsDeleted);
        // CODE CŨ: if (!isAccountExist) throw new Exception("Tài khoản (Account) không tồn tại.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        if (!isAccountExist) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Tài khoản (Account) không tồn tại.");

        var newLog = new Repository.Entity.AuditLog
        {
            // CODE CŨ: AccountId = request.AccountId,
            // FIX B33: log thủ công được đánh dấu client, không giả làm audit tự động.
            AccountId = accountId,
            ActionType = request.ActionType,
            Description = "[client] " + request.Description,
            Timestamp = DateTimeOffset.UtcNow
        };

        _context.AuditLogs.Add(newLog);
        await _context.SaveChangesAsync();

        // Query lại Account để lấy Username trả về cho DTO
        // CODE CŨ: FindAsync(request.AccountId).
        var account = await _context.Accounts.FindAsync(accountId); // FIX B33

        return new Response.AuditLogResponse
        {
            Id = newLog.Id,
            AccountId = newLog.AccountId,
            Username = account != null ? account.Username : string.Empty,
            ActionType = newLog.ActionType,
            Description = newLog.Description,
            Timestamp = newLog.Timestamp
        };
    }

    // FIX B33: phân trang và lọc tại DB, khoảng thời gian tính theo Timestamp.
    public async Task<Base.Response.PageResult<Response.AuditLogResponse>> SearchAsync(
        int pageIndex, int pageSize, DateTimeOffset? from, DateTimeOffset? to, Guid? accountId, string? actionType)
    {
        if (pageIndex < 1 || pageSize < 1 || pageSize > 500 || (long)(pageIndex - 1) * pageSize > int.MaxValue)
            throw new ArgumentException("Phân trang không hợp lệ.");
        if (from > to) throw new ArgumentException("Thời gian bắt đầu phải trước thời gian kết thúc.");
        // FIX B33: PostgreSQL timestamptz yêu cầu DateTimeOffset UTC.
        from = from?.ToUniversalTime();
        to = to?.ToUniversalTime();
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.Timestamp >= from.Value);
        if (to.HasValue) query = query.Where(x => x.Timestamp <= to.Value);
        if (accountId.HasValue) query = query.Where(x => x.AccountId == accountId.Value);
        if (!string.IsNullOrWhiteSpace(actionType)) query = query.Where(x => x.ActionType == actionType);
        var total = await query.CountAsync();
        var data = await query.OrderByDescending(x => x.Timestamp).ThenBy(x => x.Id)
            .Skip((pageIndex - 1) * pageSize).Take(pageSize).Select(x => new Response.AuditLogResponse {
                Id = x.Id, AccountId = x.AccountId, Username = x.Account.Username,
                ActionType = x.ActionType, Description = x.Description, Timestamp = x.Timestamp
            }).ToListAsync();
        return new() { Data = data, TotalItems = total, PageIndex = pageIndex, PageSize = pageSize };
    }
}
