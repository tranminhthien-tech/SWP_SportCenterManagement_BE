using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.TrainingResult;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api")]
public class TrainingResultController : ControllerBase
{
    // FIX B13: không cần gọi N request theo từng học viên.
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
    [HttpGet("coaches/me/training-results")]
    public async Task<IActionResult> GetCoachResults([FromQuery] Guid? memberId, [FromQuery] Guid? planId,
        // CODE CŨ: [FromQuery] int pageSize = 10, [FromQuery] int pageIndex = 1)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10, [FromQuery] int pageIndex = 1)
        => Ok(await _service.GetCoachResultsAsync(memberId, planId, pageSize, pageIndex));
    private readonly IService _service;

    public TrainingResultController(IService service)
    {
        _service = service;
    }

    // GET: api/training-results/{id}
    // Lấy chi tiết kết quả tập luyện
    [Authorize(Policy = JwtExtensions.ManagerOrCoachPolicy)]
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
    [Authorize(Policy = JwtExtensions.MemberPolicy)]
    [HttpGet("members/me/training-results")]
    public async Task<IActionResult> GetMyTrainingResults(
        [FromQuery] string? searchTerm,
        // CODE CŨ: [FromQuery] int pageSize = 10,
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10,
        // CODE CŨ: [FromQuery] int pageIndex = 1)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 2147483647)] int pageIndex = 1)
    {
        var result = await _service.GetMyTrainingResultsAsync(
            searchTerm,
            pageSize,
            pageIndex);

        return Ok(result);
    }

    // GET: api/coaches/me/members/{memberId}/training-results
    // Lấy tiến độ tập luyện của một Member
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
    [HttpGet("coaches/me/members/{memberId:guid}/training-results")]
    public async Task<IActionResult> GetByMemberId(
        Guid memberId,
        [FromQuery] string? searchTerm,
        // CODE CŨ: [FromQuery] int pageSize = 10,
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 500)] int pageSize = 10,
        // CODE CŨ: [FromQuery] int pageIndex = 1)
        // FIX B19: Swagger công bố giới hạn và MVC validate trước query.
        [FromQuery, System.ComponentModel.DataAnnotations.Range(1, 2147483647)] int pageIndex = 1)
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
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
    [HttpPost("training-results")]
    public async Task<IActionResult> Create(
        [FromBody] Request.TrainingResultRequest request)
    {
        var result = await _service.CreateAsync(request);

        return Ok(result);
    }

    // PUT: api/training-results/{id}
    // Cập nhật kết quả tập luyện
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
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
    [Authorize(Policy = JwtExtensions.CoachPolicy)]
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
