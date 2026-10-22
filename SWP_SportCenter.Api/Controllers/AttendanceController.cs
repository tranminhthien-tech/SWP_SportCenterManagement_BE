using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Policy = "AttendanceViewPolicy")]
    [HttpGet("session/{sessionId:guid}")]
    public async Task<IActionResult> GetBySessionId(Guid sessionId)
    {
        var result = await _attendanceService
            .GetBySessionIdAsync(sessionId);

        return Ok(result);
    }

    // GET: api/attendances/member/{memberId}
    // Lấy lịch sử điểm danh của một học viên
    // CODE CŨ: chỉ AttendanceViewPolicy.
    // FIX B5: cho Member xem với kiểm tra ownership tại Service.
    [Authorize(Roles = "Member,Coach,Receptionist,CenterManager")]
    [HttpGet("member/{memberId:guid}")]
    public async Task<IActionResult> GetByMemberId(Guid memberId)
    {
        var result = await _attendanceService
            .GetByMemberIdAsync(memberId);

        return Ok(result);
    }

    // POST: api/attendances
    // Điểm danh học viên
    // CODE CŨ: CoachAndReceptionPolicy không có Manager.
    // FIX B15: quyền ghi thống nhất với quyền xem.
    [Authorize(Policy = "AttendanceViewPolicy")]
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
    // CODE CŨ: CoachAndReceptionPolicy.
    // FIX B15: Manager cũng được sửa điểm danh.
    [Authorize(Policy = "AttendanceViewPolicy")]
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
