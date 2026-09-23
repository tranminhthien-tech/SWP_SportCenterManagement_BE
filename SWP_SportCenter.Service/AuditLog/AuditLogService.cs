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

    public AuditLogService(AppDbContext context)
    {
        _context = context;
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
        var isAccountExist = await _context.Accounts.AnyAsync(a => a.Id == request.AccountId);
        if (!isAccountExist) throw new Exception("Tài khoản (Account) không tồn tại.");

        var newLog = new Repository.Entity.AuditLog
        {
            AccountId = request.AccountId,
            ActionType = request.ActionType,
            Description = request.Description,
            Timestamp = DateTimeOffset.UtcNow
        };

        _context.AuditLogs.Add(newLog);
        await _context.SaveChangesAsync();

        // Query lại Account để lấy Username trả về cho DTO
        var account = await _context.Accounts.FindAsync(request.AccountId);

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
}