using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.ClassBooking;

public class ClassBookingService : IClassBookingService
{
    private readonly AppDbContext _context;

    public ClassBookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response.BookingResponse>> GetMemberBookingsAsync(Guid memberId)
    {
        return await _context.ClassBookings
            .AsNoTracking()
            .Include(cb => cb.Class)
            .Include(cb => cb.Member)
            .Where(cb => cb.MemberId == memberId)
            .OrderByDescending(cb => cb.BookingDate)
            .Select(cb => new Response.BookingResponse
            {
                Id = cb.Id,
                MemberId = cb.MemberId,
                MemberName = cb.Member != null ? cb.Member.FullName : string.Empty,
                ClassId = cb.ClassId,
                ClassName = cb.Class != null ? cb.Class.ClassName : string.Empty,
                ClassStartDate = cb.Class != null ? cb.Class.StartDate : DateTime.MinValue,
                BookingDate = cb.BookingDate,
                Status = cb.Status
            })
            .ToListAsync();
    }

    public async Task<Response.BookingResponse> BookClassAsync(Request.CreateBookingRequest request)
    {
        // 1. Kiểm tra Lớp học có tồn tại và còn mở không
        var classEntity = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId);
        if (classEntity == null) 
            throw new Exception("Lớp học không tồn tại.");
            
        if (classEntity.Status == ClassStatus.Completed || classEntity.Status == ClassStatus.Cancelled)
            throw new Exception("Lớp học này đã kết thúc hoặc bị hủy, không thể đăng ký.");

        // 2. Kiểm tra xem Học viên đã đăng ký lớp này chưa (chống Duplicate Booking)
        var isAlreadyBooked = await _context.ClassBookings
            .AnyAsync(cb => cb.MemberId == request.MemberId && cb.ClassId == request.ClassId);
            
        if (isAlreadyBooked) 
            throw new Exception("Bạn đã đăng ký tham gia lớp học này rồi.");

        // 3. Kiểm tra số lượng học viên tối đa (Capacity Check)
        var currentBookingsCount = await _context.ClassBookings
            .CountAsync(cb => cb.ClassId == request.ClassId);
            
        if (currentBookingsCount >= classEntity.MaxCapacity)
            throw new Exception("Lớp học đã đạt số lượng học viên tối đa.");

        // 4. Tiến hành lưu đăng ký
        var newBooking = new Repository.Entity.ClassBooking
        {
            MemberId = request.MemberId,
            ClassId = request.ClassId,
            BookingDate = DateTimeOffset.UtcNow,
            Status = BookingStatus.Booked
        };

        _context.ClassBookings.Add(newBooking);
        await _context.SaveChangesAsync();

        // 5. Trả về kết quả cho API
        var savedBooking = await _context.ClassBookings
            .Include(cb => cb.Class)
            .Include(cb => cb.Member)
            .FirstAsync(cb => cb.Id == newBooking.Id);

        return new Response.BookingResponse
        {
            Id = savedBooking.Id,
            MemberId = savedBooking.MemberId,
            MemberName = savedBooking.Member.FullName,
            ClassId = savedBooking.ClassId,
            ClassName = savedBooking.Class.ClassName,
            ClassStartDate = savedBooking.Class.StartDate,
            BookingDate = savedBooking.BookingDate,
            Status = savedBooking.Status
        };
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid memberId)
    {
        // Yêu cầu phải khớp cả bookingId và memberId để tránh việc user này xóa nhầm/cố tình xóa booking của user khác
        var booking = await _context.ClassBookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.MemberId == memberId);
        
        if (booking == null) return false;

        // Tránh xóa nếu học viên đã điểm danh/tham gia lớp này (ràng buộc dữ liệu thực tế)
        if (booking.Status == BookingStatus.Attended)
            throw new Exception("Không thể hủy đăng ký vì bạn đã được xác nhận tham gia lớp học này.");

        // Thực hiện xóa cứng (DELETE) khỏi database
        _context.ClassBookings.Remove(booking);
        await _context.SaveChangesAsync();

        return true;
    }
}