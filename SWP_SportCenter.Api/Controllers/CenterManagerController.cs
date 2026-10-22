using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.CenterManager;
using SWP_SportCenter.Service.Exceptions;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/center-managers")]
public class CenterManagerController : ControllerBase
{
    private readonly ICenterManagerService _centerManagerService;

    public CenterManagerController(
        ICenterManagerService centerManagerService)
    {
        _centerManagerService = centerManagerService;
    }

    // GET: api/center-managers
    // Lấy danh sách quản lý trung tâm
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _centerManagerService.GetAllAsync();

        return Ok(result);
    }

    // GET: api/center-managers/{id}
    // Lấy thông tin quản lý theo ID
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _centerManagerService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Center manager not found"
            });
        }

        return Ok(result);
    }

    // GET: api/center-managers/account/{accountId}
    // Lấy hồ sơ quản lý theo AccountId
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpGet("account/{accountId:guid}")]
    public async Task<IActionResult> GetByAccountId(Guid accountId)
    {
        var result = await _centerManagerService
            .GetByAccountIdAsync(accountId);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Center manager not found"
            });
        }

        return Ok(result);
    }

    // POST: api/center-managers
    // Tạo hồ sơ quản lý trung tâm
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateCenterManagerRequest request)
    {
        // CODE CŨ: gọi CreateAsync và trả CreatedAtAction trực tiếp.
        // FIX 2026-10-22: lỗi nghiệp vụ trả 400 JSON cho FE.
        try
        {
            var result = await _centerManagerService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (BusinessException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/center-managers/{id}
    // Cập nhật hồ sơ quản lý
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateCenterManagerRequest request)
    {
        var result = await _centerManagerService.UpdateAsync(id, request);

        if (!result)
        {
            return NotFound(new
            {
                message = "Center manager not found"
            });
        }

        return NoContent();
    }

    // DELETE: api/center-managers/{id}
    // Xóa hồ sơ quản lý
    [Authorize(Policy = JwtExtensions.CenterManagerPolicy)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _centerManagerService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Center manager not found"
            });
        }

        return NoContent();
    }
}
