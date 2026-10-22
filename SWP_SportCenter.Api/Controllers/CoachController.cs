using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.Coach;
using SWP_SportCenter.Service.Exceptions;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/coaches")]
public class CoachController : ControllerBase
{
    private readonly ICoachService _service;

    public CoachController(
        ICoachService service)
    {
        _service = service;

    }

    // GET: api/coaches
    // Lấy danh sách tất cả Coach
    // CODE CŨ: chỉ ManagerOrReceptionistPolicy.
    // FIX B9: khách được xem danh sách Coach, không lộ email/phone.
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        if (User.IsInRole("CenterManager") || User.IsInRole("Receptionist")) return Ok(result);
        // FIX B9: response công khai chỉ chứa thông tin chuyên môn.
        return Ok(result.Select(c => new { c.Id, c.FullName, c.Avatar, c.Specialization, c.ExperienceYears }));
    }

    // GET: api/coaches/{id}
    // Lấy thông tin Coach theo ID
    // CODE CŨ: chỉ ManagerOrReceptionistPolicy.
    // FIX B9: chi tiết Coach công khai.
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound("Không tìm thấy huấn luyện viên.");

        if (User.IsInRole("CenterManager") || User.IsInRole("Receptionist")) return Ok(result);
        // FIX B9: ẩn thông tin liên hệ và AccountId ở response công khai.
        return Ok(new { result.Id, result.FullName, result.Avatar, result.Specialization, result.ExperienceYears });
    }

    // POST: api/coaches
    // Tạo hồ sơ Coach mới
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateCoachRequest request)
    {
        // CODE CŨ: var result = await _service.CreateAsync(request);
        // CODE CŨ: return Ok(result);
        // FIX 2026-10-22: bắt riêng lỗi nghiệp vụ và trả { message } để FE hiển thị.
        try
        {
            var result = await _service.CreateAsync(request);
            return Ok(result);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/coaches/{id}
    // Cập nhật thông tin Coach
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateCoachRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
            return NotFound("Không tìm thấy huấn luyện viên.");

        return Ok("Cập nhật thông tin huấn luyện viên thành công.");
    }

    // DELETE: api/coaches/{id}
    // Xóa Coach
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
            return NotFound("Không tìm thấy huấn luyện viên.");

        return Ok("Xóa huấn luyện viên thành công.");
    }

    // GET: api/coaches/me
    // Lấy thông tin Coach đang đăng nhập
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var accountId = GetCurrentAccountId();

        if (accountId == null)
            return Unauthorized("Không tìm thấy AccountId trong token.");

        var result = await _service.GetByAccountIdAsync(accountId.Value);

        if (result == null)
            return NotFound("Không tìm thấy hồ sơ huấn luyện viên.");

        return Ok(result);
    }

    // GET: api/coaches/me/classes
    // Lấy danh sách lớp học Coach đang phụ trách
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
    [HttpGet("me/classes")]
    public async Task<IActionResult> GetMyClasses()
    {
        var accountId = GetCurrentAccountId();

        if (accountId == null)
            return Unauthorized("Không tìm thấy AccountId trong token.");

        var coach = await _service.GetByAccountIdAsync(accountId.Value);

        if (coach == null)
            return NotFound("Không tìm thấy hồ sơ huấn luyện viên.");

        var result = await _service.GetCoachClassesAsync(coach.Id);

        return Ok(result);
    }

    // GET: api/coaches/me/members
    // Lấy danh sách học viên Coach đang phụ trách
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
    [HttpGet("me/members")]
    public async Task<IActionResult> GetMyMembers()
    {
        var accountId = GetCurrentAccountId();

        if (accountId == null)
            return Unauthorized("Không tìm thấy AccountId trong token.");

        var coach = await _service.GetByAccountIdAsync(accountId.Value);

        if (coach == null)
            return NotFound("Không tìm thấy hồ sơ huấn luyện viên.");

        var result = await _service.GetCoachMembersAsync(coach.Id);

        return Ok(result);
    }

    // Lấy AccountId từ JWT token
    private Guid? GetCurrentAccountId()
    {
        var accountIdClaim = User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (Guid.TryParse(accountIdClaim, out var accountId))
            return accountId;

        return null;
    }
}
