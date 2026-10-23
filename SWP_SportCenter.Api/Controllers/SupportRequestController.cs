using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Service.Support;

namespace SWP_SportCenter.Controllers;

// FIX B31: API yêu cầu hỗ trợ; không cho Coach đọc hay xử lý.
[ApiController, Route("api/support-requests")]
public sealed class SupportRequestController(SupportRequestService service) : ControllerBase
{
    [HttpPost, Authorize(Policy = "MemberPolicy")]
    public async Task<IActionResult> Create(CreateSupportRequest request)
        => StatusCode(201, await service.CreateAsync(request));

    [HttpGet, Authorize(Roles = "Member,Receptionist,CenterManager")]
    public async Task<IActionResult> Get(int pageIndex = 1, int pageSize = 20,
        Guid? memberId = null, SupportRequestStatus? status = null)
        => Ok(await service.GetAsync(pageIndex, pageSize, memberId, status));

    [HttpPut("{id:guid}"), Authorize(Policy = "ManagerOrReceptionistPolicy")]
    public async Task<IActionResult> Update(Guid id, UpdateSupportRequest request)
        => await service.UpdateAsync(id, request) ? NoContent() : NotFound(new { message = "Không tìm thấy yêu cầu hỗ trợ." });
}
