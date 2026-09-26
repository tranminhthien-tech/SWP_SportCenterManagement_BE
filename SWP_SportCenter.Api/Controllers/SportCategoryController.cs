using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.SportCategory;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/categories")]
public class SportCategoryController : ControllerBase
{
    private readonly ISportCategoryService _service;

    public SportCategoryController(ISportCategoryService service)
    {
        _service = service;
    }

    // GET: api/categories
    // Lấy danh sách bộ môn
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    // GET: api/categories/{id}
    // Lấy thông tin chi tiết bộ môn
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound("Không tìm thấy bộ môn.");

        return Ok(result);
    }

    // POST: api/categories
    // Tạo bộ môn mới
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateSportCategoryRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/categories/{id}
    // Cập nhật thông tin bộ môn
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateSportCategoryRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
            return NotFound("Không tìm thấy bộ môn.");

        return Ok("Cập nhật bộ môn thành công.");
    }

    // DELETE: api/categories/{id}
    // Xóa bộ môn
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
            return NotFound("Không tìm thấy bộ môn.");

        return Ok("Xóa bộ môn thành công.");
    }
}