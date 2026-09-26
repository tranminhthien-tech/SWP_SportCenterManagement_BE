using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Member;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/members")]
public class MemberController: ControllerBase
{
    private readonly IService _service;
    public MemberController(IService service)
    {
        _service = service;
    }
    
    // GET /api/members
    // Lấy danh sách / tìm kiếm Member
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(
            pageIndex,
            pageSize,
            searchTerm);

        return Ok(result);
    }

    // GET /api/members/{id}
    // Lấy chi tiết Member
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound("Member not found");
        }

        return Ok(result);
    }

    // POST /api/members
    // Tạo Member
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.MemberRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT /api/members/{id}
    // Cập nhật Member
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] Request.MemberRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
        {
            return NotFound("Member not found");
        }

        return Ok(result);
    }

    // DELETE /api/members/{id}
    // Xóa mềm Member
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
        {
            return NotFound("Member not found");
        }

        return Ok(result);
    }

    // GET /api/members/me
    // Xem profile của Member đang đăng nhập
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var result = await _service.GetMyProfileAsync();

        if (result == null)
        {
            return NotFound("Member profile not found");
        }

        return Ok(result);
    }

    // PUT /api/members/me
    // Cập nhật profile của Member đang đăng nhập
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] Request.MemberRequest request)
    {
        var result = await _service.UpdateMyProfileAsync(request);

        if (!result)
        {
            return NotFound("Member profile not found");
        }

        return Ok(result);
    }

    // GET /api/members/{id}/summary
    // Lấy tổng quan Member
    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> GetSummary(
        [FromRoute] Guid id)
    {
        var result = await _service.GetSummaryAsync(id);

        if (result == null)
        {
            return NotFound("Member not found");
        }

        return Ok(result);
    }
}