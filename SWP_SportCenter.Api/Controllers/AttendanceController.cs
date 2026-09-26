using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Attendance;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/attendances")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;

    public AttendanceController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    // GET: api/attendances/session/{sessionId}
    // Lấy danh sách điểm danh của một buổi học
    [HttpGet("session/{sessionId:guid}")]
    public async Task<IActionResult> GetBySessionId(Guid sessionId)
    {
        var result = await _attendanceService
            .GetBySessionIdAsync(sessionId);

        return Ok(result);
    }

    // GET: api/attendances/member/{memberId}
    // Lấy lịch sử điểm danh của một học viên
    [HttpGet("member/{memberId:guid}")]
    public async Task<IActionResult> GetByMemberId(Guid memberId)
    {
        var result = await _attendanceService
            .GetByMemberIdAsync(memberId);

        return Ok(result);
    }

    // POST: api/attendances
    // Điểm danh học viên
    [HttpPost]
    public async Task<IActionResult> RecordAttendance(
        [FromBody] Request.RecordAttendanceRequest request)
    {
        var result = await _attendanceService
            .RecordAttendanceAsync(request);

        return Ok(result);
    }

    // PUT: api/attendances/{id}
    // Cập nhật trạng thái điểm danh
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateAttendance(
        Guid id,
        [FromBody] Request.UpdateAttendanceRequest request)
    {
        var result = await _attendanceService
            .UpdateAttendanceAsync(id, request);

        if (!result)
        {
            return NotFound(new
            {
                message = "Attendance not found"
            });
        }

        return NoContent();
    }
}