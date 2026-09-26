using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.TrainingPlan;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api")]
public class TrainingPlanController : ControllerBase
{
    private readonly IService _service;

    public TrainingPlanController(IService service)
    {
        _service = service;
    }

    // GET: api/training-plans/{id}
    // Lấy chi tiết kế hoạch tập luyện
    [HttpGet("training-plans/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound("Training plan not found.");
        }

        return Ok(result);
    }

    // GET: api/coaches/me/training-plans
    // Lấy danh sách kế hoạch của coach đang đăng nhập
    [HttpGet("coaches/me/training-plans")]
    public async Task<IActionResult> GetMyTrainingPlansAsCoach(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetMyTrainingPlansAsCoachAsync(
            searchTerm,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET: api/members/me/training-plans
    // Lấy danh sách kế hoạch của member đang đăng nhập
    [HttpGet("members/me/training-plans")]
    public async Task<IActionResult> GetMyTrainingPlansAsMember(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetMyTrainingPlansAsMemberAsync(
            searchTerm,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // POST: api/training-plans
    // Tạo kế hoạch tập luyện
    [HttpPost("training-plans")]
    public async Task<IActionResult> Create(
        [FromBody] Request.TrainingPlanRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/training-plans/{id}
    // Cập nhật kế hoạch tập luyện
    [HttpPut("training-plans/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.TrainingPlanRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
        {
            return NotFound("Training plan not found.");
        }

        return Ok("Training plan updated successfully.");
    }

    // DELETE: api/training-plans/{id}
    // Xóa mềm kế hoạch tập luyện
    [HttpDelete("training-plans/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (result == "Training plan not found")
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}