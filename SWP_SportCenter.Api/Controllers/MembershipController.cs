using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Membership;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api")]
public class MembershipController : ControllerBase
{
    private readonly IService _service;

    public MembershipController(IService membershipService)
    {
        _service = membershipService;
    }

    // GET /api/members/me/memberships
    // Lấy danh sách Membership của Member đang đăng nhập
    [HttpGet("members/me/memberships")]
    public async Task<IActionResult> GetMyMemberships(
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetMyMembershipsAsync(
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET /api/members/{memberId}/memberships
    // Lấy danh sách Membership của một Member
    [HttpGet("members/{memberId:guid}/memberships")]
    public async Task<IActionResult> GetByMemberId(
        [FromRoute] Guid memberId,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetByMemberIdAsync(
            memberId,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET /api/memberships/{id}
    // Lấy chi tiết Membership
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
    [HttpPost("memberships")]
    public async Task<IActionResult> Create(
        [FromBody] Request.MembershipRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // POST /api/memberships/{id}/renew
    // Gia hạn Membership
    [HttpPost("memberships/{id:guid}/renew")]
    public async Task<IActionResult> Renew(
        [FromRoute] Guid id)
    {
        var result = await _service.RenewAsync(id);

        if (result == null)
        {
            return NotFound("Membership not found");
        }

        return Ok(result);
    }

    // POST /api/memberships/{id}/cancel
    // Hủy Membership
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