using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.MembershipPackage;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/membership-packages")]
public class MembershipPackageController : ControllerBase
{
    private readonly IService _service;

    public MembershipPackageController(IService service)
    {
        _service = service;
    }

    // GET: api/membership-packages
    // Lấy danh sách gói tập, có phân trang và tìm kiếm
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetAllAsync(
            searchTerm,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET: api/membership-packages/{id}
    // Lấy chi tiết gói tập
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound("Membership package not found.");
        }

        return Ok(result);
    }

    // POST: api/membership-packages
    // Tạo gói tập mới
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.MembershipPackageRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/membership-packages/{id}
    // Cập nhật gói tập
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.MembershipPackageRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
        {
            return NotFound("Membership package not found.");
        }

        return Ok("Membership package updated successfully.");
    }

    // DELETE: api/membership-packages/{id}
    // Xóa mềm gói tập
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
        {
            return NotFound("Membership package not found.");
        }

        return Ok("Membership package deleted successfully.");
    }
}
