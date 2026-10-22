using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.ClassBooking;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/class-bookings")]
public class ClassBookingController : ControllerBase
{
    private readonly IClassBookingService _classBookingService;

    public ClassBookingController(IClassBookingService classBookingService)
    {
        _classBookingService = classBookingService;
    }

    // GET: api/class-bookings/member/{memberId}
    // Lấy danh sách lớp học mà Member đã đăng ký
    // CODE CŨ: [Authorize(Policy = JwtExtensions.StaffPolicy)]
    // FIX B4: Member xem chính mình, Service kiểm tra ownership.
    [Authorize(Roles = "Member,Coach,Receptionist,CenterManager")]
    [HttpGet("member/{memberId:guid}")]
    public async Task<IActionResult> GetMemberBookings(Guid memberId)
    {
        var result = await _classBookingService.GetMemberBookingsAsync(memberId);

        return Ok(result);
    }

    // POST: api/class-bookings
    // Đăng ký tham gia lớp học
    // CODE CŨ: chỉ MemberPolicy.
    // FIX B8: Lễ tân/Manager được đặt hộ học viên.
    [Authorize(Roles = "Member,Receptionist,CenterManager")]
    [HttpPost]
    public async Task<IActionResult> BookClass(
        [FromBody] Request.CreateBookingRequest request)
    {
        var result = await _classBookingService.BookClassAsync(request);

        return Ok(result);
    }

    // DELETE: api/class-bookings/{bookingId}/member/{memberId}
    // Hủy đăng ký lớp học
    // CODE CŨ: chỉ MemberPolicy.
    // FIX B8: Lễ tân/Manager được hủy hộ học viên.
    [Authorize(Roles = "Member,Receptionist,CenterManager")]
    [HttpDelete("{bookingId:guid}/member/{memberId:guid}")]
    public async Task<IActionResult> CancelBooking(
        Guid bookingId,
        Guid memberId)
    {
        var result = await _classBookingService.CancelBookingAsync(
            bookingId,
            memberId);

        if (!result)
        {
            return NotFound(new
            {
                message = "Booking not found"
            });
        }

        return NoContent();
    }
}
