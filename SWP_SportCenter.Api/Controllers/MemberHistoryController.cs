using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Security;
using SWP_SportCenter.Service.ClassBooking;
using SWP_SportCenter.Service.Attendance;

namespace SWP_SportCenter.Controllers;

// FIX B4 B5: route /me không nhận MemberId từ client.
[ApiController]
[Route("api/members/me")]
[Authorize(Roles = "Member")]
public class MemberHistoryController(CurrentActor actor, IClassBookingService bookings,
    IAttendanceService attendance) : ControllerBase
{
    [HttpGet("class-bookings")]
    public async Task<IActionResult> Bookings() => Ok(await bookings.GetMemberBookingsAsync(await actor.MemberIdAsync()));

    [HttpGet("attendances")]
    public async Task<IActionResult> Attendances() => Ok(await attendance.GetByMemberIdAsync(await actor.MemberIdAsync()));
}
