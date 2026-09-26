using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.ClassSession;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/class-sessions")]
public class ClassSessionController : ControllerBase
{
    private readonly IClassSessionService _classSessionService;

    public ClassSessionController(IClassSessionService classSessionService)
    {
        _classSessionService = classSessionService;
    }

    // GET: api/class-sessions
    // Lấy danh sách tất cả buổi học
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _classSessionService.GetAllAsync();

        return Ok(result);
    }

    // GET: api/class-sessions/{id}
    // Lấy thông tin buổi học theo ID
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _classSessionService.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound(new
            {
                message = "Class session not found"
            });
        }

        return Ok(result);
    }

    // GET: api/class-sessions/class/{classId}
    // Lấy danh sách buổi học của một lớp
    [HttpGet("class/{classId:guid}")]
    public async Task<IActionResult> GetByClassId(Guid classId)
    {
        var result = await _classSessionService.GetByClassIdAsync(classId);

        return Ok(result);
    }

    // POST: api/class-sessions
    // Tạo buổi học mới
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] Request.CreateClassSessionRequest request)
    {
        var result = await _classSessionService.CreateAsync(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result
        );
    }

    // PUT: api/class-sessions/{id}
    // Cập nhật buổi học
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.UpdateClassSessionRequest request)
    {
        var result = await _classSessionService.UpdateAsync(id, request);

        if (!result)
        {
            return NotFound(new
            {
                message = "Class session not found"
            });
        }

        return NoContent();
    }

    // DELETE: api/class-sessions/{id}
    // Xóa buổi học
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _classSessionService.DeleteAsync(id);

        if (!result)
        {
            return NotFound(new
            {
                message = "Class session not found"
            });
        }

        return NoContent();
    }
}