using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.ClassBooking;

public interface IClassBookingService
{
    // Lấy danh sách các lớp học mà một học viên cụ thể đã đăng ký
    Task<IEnumerable<Response.BookingResponse>> GetMemberBookingsAsync(Guid memberId);
    
    // Học viên đăng ký lớp mới
    Task<Response.BookingResponse> BookClassAsync(Request.CreateBookingRequest request);
    
    // Học viên hủy đăng ký lớp
    Task<bool> CancelBookingAsync(Guid bookingId, Guid memberId);
}