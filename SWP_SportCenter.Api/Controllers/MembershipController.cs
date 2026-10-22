using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.Membership;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api")]
public class MembershipController : ControllerBase
{
    // FIX B20: bổ sung API sửa/xóa mềm membership.
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPut("memberships/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Request.UpdateMembershipRequest request)
        => await _service.UpdateAsync(id, request) ? NoContent() : NotFound(new { message = "Membership not found" });

    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpDelete("memberships/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => await _service.DeleteAsync(id) ? NoContent() : NotFound(new { message = "Membership not found" });
    private readonly IService _service;

    public MembershipController(IService membershipService)
    {
        _service = membershipService;
    }

    // GET /api/memberships
    // Lễ tân/quản lý lấy danh sách membership tổng quát, có phân trang và bộ lọc.
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet("memberships")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] SWP_SportCenter.Repository.Enum.MembershipStatus? status,
        [FromQuery] Guid? memberId,
        [FromQuery] Guid? packageId,
        // CODE CŨ: [FromQuery] int pageSize = 10,
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10,
        // CODE CŨ: [FromQuery] int pageIndex = 1)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 2147483647)] int pageIndex = 1)
    {
        var result = await _service.GetAllAsync(
            searchTerm,
            status,
            memberId,
            packageId,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET /api/members/me/memberships
    // Lấy danh sách Membership của Member đang đăng nhập
    [Authorize(Policy = JwtExtensions.MemberPolicy)]
    [HttpGet("members/me/memberships")]
    public async Task<IActionResult> GetMyMemberships(
        // CODE CŨ: [FromQuery] int pageSize = 10,
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10,
        // CODE CŨ: [FromQuery] int pageIndex = 1)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 2147483647)] int pageIndex = 1)
    {
        var result = await _service.GetMyMembershipsAsync(
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET /api/members/{memberId}/memberships
    // Lấy danh sách Membership của một Member
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet("members/{memberId:guid}/memberships")]
    public async Task<IActionResult> GetByMemberId(
        [FromRoute] Guid memberId,
        // CODE CŨ: [FromQuery] int pageSize = 10,
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10,
        // CODE CŨ: [FromQuery] int pageIndex = 1)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 2147483647)] int pageIndex = 1)
    {
        var result = await _service.GetByMemberIdAsync(
            memberId,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET /api/memberships/{id}
    // Lấy chi tiết Membership
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet("memberships/{id:guid}")]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound("Membership not found");
        }

        return Ok(result);
    }

    // GET /api/memberships/{id}/status
    // Kiểm tra trạng thái Membership
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet("memberships/{id:guid}/status")]
    public async Task<IActionResult> GetStatus(
        [FromRoute] Guid id)
    {
        var result = await _service.GetStatusAsync(id);

        if (result == null)
        {
            return NotFound("Membership not found");
        }

        return Ok(result);
    }

    // POST /api/memberships
    // Đăng ký / mua Membership
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPost("memberships")]
    public async Task<IActionResult> Create(
        [FromBody] Request.MembershipRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // POST /api/memberships/{id}/renew
    // Gia hạn Membership
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPost("memberships/{id:guid}/renew")]
    public async Task<IActionResult> Renew(
        [FromRoute] Guid id,
        [FromQuery] Guid? receptionistId = null)
    {
        // CODE CŨ: _service.RenewAsync(id);
        // FIX B20: lập hóa đơn mới cho kỳ gia hạn, không sửa kỳ cũ.
        var result = await _service.RenewAsync(id, receptionistId);

        if (result == null)
        {
            return NotFound("Membership not found");
        }

        return Ok(result);
    }

    // POST /api/memberships/{id}/cancel
    // Hủy Membership
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPost("memberships/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid id)
    {
        var result = await _service.CancelAsync(id);

        if (!result)
        {
            return NotFound("Membership not found");
        }

        return Ok(result);
    }
}
