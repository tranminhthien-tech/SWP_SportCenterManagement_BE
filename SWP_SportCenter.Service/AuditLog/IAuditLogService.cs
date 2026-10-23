using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.AuditLog;

public interface IAuditLogService
{
    // FIX B33: endpoint danh sách có phân trang và bộ lọc.
    Task<Base.Response.PageResult<Response.AuditLogResponse>> SearchAsync(int pageIndex, int pageSize,
        DateTimeOffset? from, DateTimeOffset? to, Guid? accountId, string? actionType);
    // Lấy toàn bộ log của hệ thống (dành cho Admin)
    Task<IEnumerable<Response.AuditLogResponse>> GetAllAsync();
    
    // Lấy chi tiết 1 log cụ thể
    Task<Response.AuditLogResponse?> GetByIdAsync(Guid id);
    
    // Lấy lịch sử hoạt động của 1 tài khoản cụ thể
    Task<IEnumerable<Response.AuditLogResponse>> GetByAccountIdAsync(Guid accountId);
    
    // Ghi log mới
    Task<Response.AuditLogResponse> LogAsync(Request.CreateAuditLogRequest request);
}
