using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Classes;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/classes")]
public class ClassController : ControllerBase
{
    private readonly IClassService _service;

    public ClassController(IClassService service)
    {
        _service = service;
    }

    // GET: api/classes
    // Lấy danh sách lớp học
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();

        return Ok(result);
    }

    // GET: api/classes/{id}
    // Lấy chi tiết lớp học
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound("Class not found.");
        }

        return Ok(result);
    }

    // POST: api/classes
    // Tạo lớp học
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateClassRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/classes/{id}
    // Cập nhật lớp học
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateClassRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
        {
            return NotFound("Class not found.");
        }

        return Ok("Class updated successfully.");
    }

    // DELETE: api/classes/{id}
    // Xóa lớp học
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (!result)
        {
            return NotFound("Class not found.");
        }

        return Ok("Class deleted successfully.");
    }

    // GET: api/classes/{classId}/members
    // Lấy danh sách học viên của lớp
    [HttpGet("{classId:guid}/members")]
    public async Task<IActionResult> GetMembersByClassId(
        Guid classId)
    {
        var result = await _service.GetMembersByClassIdAsync(classId);

        return Ok(result);
    } 
}