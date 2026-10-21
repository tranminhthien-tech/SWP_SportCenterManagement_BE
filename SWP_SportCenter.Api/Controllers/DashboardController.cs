using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.Dashboard;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    // GET: api/dashboard/revenue?from=2025-01-01&to=2025-12-31
    // Báo cáo doanh thu theo khoảng thời gian
    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to)
    {
        if (from > to)
            return BadRequest("Ngày bắt đầu phải nhỏ hơn ngày kết thúc.");

        var result = await _service.GetRevenueReportAsync(from, to);
        return Ok(result);
    }

    // GET: api/dashboard/members/summary
    // Thống kê thành viên
    [HttpGet("members/summary")]
    public async Task<IActionResult> GetMemberSummary()
    {
        var result = await _service.GetMemberSummaryAsync();
        return Ok(result);
    }

    // GET: api/dashboard/memberships/summary
    // Thống kê gói tập
    [HttpGet("memberships/summary")]
    public async Task<IActionResult> GetMembershipSummary()
    {
        var result = await _service.GetMembershipSummaryAsync();
        return Ok(result);
    }

    // GET: api/dashboard/classes/summary
    // Thống kê lớp học
    [HttpGet("classes/summary")]
    public async Task<IActionResult> GetClassSummary()
    {
        var result = await _service.GetClassSummaryAsync();
        return Ok(result);
    }
}
