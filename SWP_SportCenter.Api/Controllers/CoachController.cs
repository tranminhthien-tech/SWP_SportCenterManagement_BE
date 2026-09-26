using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Coach;

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
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    // GET: api/coaches/{id}
    // Lấy thông tin Coach theo ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound("Không tìm thấy huấn luyện viên.");

        return Ok(result);
    }

    // POST: api/coaches
    // Tạo hồ sơ Coach mới
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateCoachRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/coaches/{id}
    // Cập nhật thông tin Coach
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