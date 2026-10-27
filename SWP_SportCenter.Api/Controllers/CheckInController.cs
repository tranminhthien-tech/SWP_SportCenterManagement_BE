using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.CheckIns;
namespace SWP_SportCenter.Controllers;

// FIX B47: API vào cửa riêng, giữ nguyên CoachPolicy cho điểm danh lớp.
[ApiController, Route("api/check-ins")]
public sealed class CheckInController(CheckInService service) : ControllerBase
{
    [HttpPost, Authorize(Policy = "ManagerOrReceptionistPolicy")]
    public async Task<IActionResult> Create(CreateCheckIn request) => StatusCode(201, await service.CreateAsync(request));
    [HttpGet, Authorize(Policy = "ManagerOrReceptionistPolicy")]
    public async Task<IActionResult> Get(DateOnly? date = null, int pageIndex = 1, int pageSize = 20)
        => Ok(await service.GetAsync(date, pageIndex, pageSize));
    [HttpGet("~/api/members/me/check-ins"), Authorize(Policy = "MemberPolicy")]
    public async Task<IActionResult> Mine(DateOnly? date = null, int pageIndex = 1, int pageSize = 20)
        => Ok(await service.GetAsync(date, pageIndex, pageSize));
}
