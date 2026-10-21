using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.AuditLog;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/audit-logs")]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    // GET: api/audit-logs
    // Lấy toàn bộ lịch sử hoạt động
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _auditLogService.GetAllAsync();

        return Ok(result);
    }

    // GET: api/audit-logs/{id}
    // Lấy chi tiết log theo ID
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _auditLogService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Audit log not found"
            });
        }

        return Ok(result);
    }

    // GET: api/audit-logs/account/{accountId}
    // Lấy lịch sử hoạt động của một tài khoản
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet("account/{accountId:guid}")]
    public async Task<IActionResult> GetByAccountId(Guid accountId)
    {
        var result = await _auditLogService
            .GetByAccountIdAsync(accountId);

        return Ok(result);
    }

    // POST: api/audit-logs
    // Ghi log hoạt động mới
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPost]
    public async Task<IActionResult> Log(
        [FromBody] Request.CreateAuditLogRequest request)
    {
        var result = await _auditLogService.LogAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result
        );
    }
}
