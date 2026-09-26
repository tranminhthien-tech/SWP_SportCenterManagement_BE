using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.TrainingResult;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api")]
public class TrainingResultController : ControllerBase
{
    private readonly IService _service;

    public TrainingResultController(IService service)
    {
        _service = service;
    }

    // GET: api/training-results/{id}
    // Lấy chi tiết kết quả tập luyện
    [HttpGet("training-results/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
        {
            return NotFound("Training result not found.");
        }

        return Ok(result);
    }

    // GET: api/members/me/training-results
    // Lấy lịch sử kết quả của Member đang đăng nhập
    [HttpGet("members/me/training-results")]
    public async Task<IActionResult> GetMyTrainingResults(
        [FromQuery] string? searchTerm,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetMyTrainingResultsAsync(
            searchTerm,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET: api/coaches/me/members/{memberId}/training-results
    // Lấy tiến độ tập luyện của một Member
    [HttpGet("coaches/me/members/{memberId:guid}/training-results")]
    public async Task<IActionResult> GetByMemberId(
        Guid memberId,
        [FromQuery] string? searchTerm,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageIndex = 1)
    {
        var result = await _service.GetByMemberIdAsync(
            memberId,
            searchTerm,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // POST: api/training-results
    // Ghi nhận kết quả và nhận xét của Coach
    [HttpPost("training-results")]
    public async Task<IActionResult> Create(
        [FromBody] Request.TrainingResultRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/training-results/{id}
    // Cập nhật kết quả tập luyện
    [HttpPut("training-results/{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] Request.TrainingResultRequest request)
    {
        var result = await _service.UpdateAsync(id, request);

        if (!result)
        {
            return NotFound("Training result not found.");
        }

        return Ok("Training result updated successfully.");
    }

    // DELETE: api/training-results/{id}
    // Xóa mềm kết quả tập luyện
    [HttpDelete("training-results/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _service.DeleteAsync(id);

        if (result == "Training result not found")
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}