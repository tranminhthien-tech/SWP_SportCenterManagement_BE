using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.Member;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/members")]
// CODE CŨ: public class MemberController: ControllerBase
public partial class MemberController: ControllerBase // FIX B22
{
    private readonly IService _service;
    public MemberController(IService service)
    {
        _service = service;
    }
    
    // GET /api/members
    // Lấy danh sách / tìm kiếm Member
    [Authorize(Policy = JwtExtensions.StaffPolicy)]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        // CODE CŨ: [FromQuery] int pageIndex = 1,
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 2147483647)] int pageIndex = 1,
        // CODE CŨ: [FromQuery] int pageSize = 10)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10)
    {
        var result = await _service.GetAllAsync(
            pageIndex,
            pageSize,
            searchTerm);

        return Ok(result);
    }

    // GET /api/members/{id}
    // Lấy chi tiết Member
    [Authorize(Policy = JwtExtensions.StaffPolicy)]
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
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPost]
    [Consumes("application/json")] // FIX B22: giữ JSON, thêm action FormData riêng.
    public async Task<IActionResult> Create(
        [FromBody] Request.MemberRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT /api/members/{id}
    // Cập nhật Member
    // CODE CŨ: [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    // FIX B24: lễ tân được sửa hồ sơ tại quầy, không mở quyền xóa.
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPut("{id:guid}")]
    [Consumes("application/json")] // FIX B22
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
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
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
    [Authorize(Policy = JwtExtensions.MemberPolicy)]
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
    [Authorize(Policy = JwtExtensions.MemberPolicy)]
    [HttpPut("me")]
    [Consumes("application/json")] // FIX B22
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
    [Authorize(Policy = JwtExtensions.StaffPolicy)]
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
