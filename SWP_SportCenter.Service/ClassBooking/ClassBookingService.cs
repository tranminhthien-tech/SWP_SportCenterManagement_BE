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
    private readonly SWP_SportCenter.Service.Security.CurrentActor _actor;
    // FIX B23 2026-10-23: clock có thể cố định trong test, production dùng UTC thật.
    private readonly TimeProvider _clock;

    // CODE CŨ: constructor chỉ nhận DbContext.
    // FIX B7: lấy MemberId từ danh tính đã xác thực.
    // CODE CŨ: constructor chỉ nhận context và actor.
    // FIX B23: clock tùy chọn giữ tương thích các caller hiện có.
    public ClassBookingService(AppDbContext context, SWP_SportCenter.Service.Security.CurrentActor actor,
        TimeProvider? clock = null)
    {
        _context = context;
        _actor = actor;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<IEnumerable<Response.BookingResponse>> GetMemberBookingsAsync(Guid memberId)
    {
        // FIX B4 B7: Member được đọc chính mình; staff đọc theo quyền controller.
        await _actor.EnsureCanViewMemberAsync(memberId);
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
        // FIX B7 B8: Member bỏ qua ID giả mạo; staff được chỉ định MemberId.
        if (_actor.IsMember) request.MemberId = await _actor.MemberIdAsync();
        // 1. Kiểm tra Lớp học có tồn tại và còn mở không
        // CODE CŨ: tìm theo ClassId mà không loại bản ghi xóa mềm.
        // FIX B7: chỉ đặt lớp còn tồn tại.
        var classEntity = await _context.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId && !c.IsDeleted);
        if (classEntity == null) 
            // CODE CŨ: throw new Exception("Lớp học không tồn tại.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lớp học không tồn tại.");
            
        if (classEntity.Status == ClassStatus.Completed || classEntity.Status == ClassStatus.Cancelled)
            // CODE CŨ: throw new Exception("Lớp học này đã kết thúc hoặc bị hủy, không thể đăng ký.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lớp học này đã kết thúc hoặc bị hủy, không thể đăng ký.");

        // CODE CŨ: chỉ kiểm tra status Completed/Cancelled rồi kiểm tra trùng booking.
        // FIX B23 2026-10-23: status Ongoing có thể chưa cập nhật dù mọi buổi đã kết thúc.
        // Date/EndTime của lịch học là giờ Việt Nam (UTC+7), không dùng timezone của server Render.
        var localNow = _clock.GetUtcNow().ToOffset(TimeSpan.FromHours(7));
        var today = DateOnly.FromDateTime(localNow.DateTime);
        var currentTime = localNow.TimeOfDay;
        var hasUnfinishedSession = await _context.ClassSessions.AsNoTracking().AnyAsync(s =>
            s.ClassId == request.ClassId && !s.IsDeleted &&
            (s.Date > today || (s.Date == today && s.EndTime > currentTime)));
        if (!hasUnfinishedSession)
            throw new SWP_SportCenter.Service.Exceptions.BusinessException(
                "Lớp học chưa có lịch hoặc tất cả buổi học đã kết thúc, không thể đăng ký.");

        // 2. Kiểm tra xem Học viên đã đăng ký lớp này chưa (chống Duplicate Booking)
        var isAlreadyBooked = await _context.ClassBookings
            // CODE CŨ: chỉ đối chiếu MemberId/ClassId.
            // FIX B14: booking hủy/xóa mềm không chiếm chỗ.
            .AnyAsync(cb => cb.MemberId == request.MemberId && cb.ClassId == request.ClassId && !cb.IsDeleted && cb.Status != BookingStatus.Cancelled);
            
        if (isAlreadyBooked) 
            // CODE CŨ: throw new Exception("Bạn đã đăng ký tham gia lớp học này rồi.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            // CODE CŨ: throw new BusinessException("Bạn đã đăng ký tham gia lớp học này rồi.");
            // FIX B34: thông báo trung tính cho cả đặt hộ; trùng booking trả 409 JSON.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException("Học viên này đã đăng ký lớp học này rồi.");

        // 3. Kiểm tra số lượng học viên tối đa (Capacity Check)
        var currentBookingsCount = await _context.ClassBookings
            // CODE CŨ: CountAsync(cb => cb.ClassId == request.ClassId).
            // FIX B14: khớp cách tính CurrentEnrolled công khai.
            .CountAsync(cb => cb.ClassId == request.ClassId && !cb.IsDeleted && cb.Status != BookingStatus.Cancelled);
            
        if (currentBookingsCount >= classEntity.MaxCapacity)
            // CODE CŨ: throw new Exception("Lớp học đã đạt số lượng học viên tối đa.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Lớp học đã đạt số lượng học viên tối đa.");
        
        if (!await _context.Members
                .AnyAsync(m => m.Id == request.MemberId && !m.IsDeleted && !m.Account.IsDeleted && m.Account.Status == AccountStatus.Active))
        {
            // CODE CŨ: throw new Exception("Học viên không tồn tại.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Học viên không tồn tại.");
        }

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
        // FIX B7: chỉ tìm booking thuộc Member hiện tại, dù URL chứa ID khác.
        if (_actor.IsMember) memberId = await _actor.MemberIdAsync();
        // Yêu cầu phải khớp cả bookingId và memberId để tránh việc user này xóa nhầm/cố tình xóa booking của user khác
        var booking = await _context.ClassBookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.MemberId == memberId);
        
        if (booking == null) return false;

        // Tránh xóa nếu học viên đã điểm danh/tham gia lớp này (ràng buộc dữ liệu thực tế)
        if (booking.Status == BookingStatus.Attended)
            // CODE CŨ: throw new Exception("Không thể hủy đăng ký vì bạn đã được xác nhận tham gia lớp học này.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Không thể hủy đăng ký vì bạn đã được xác nhận tham gia lớp học này.");

        // Thực hiện xóa cứng (DELETE) khỏi database
        _context.ClassBookings.Remove(booking);
        await _context.SaveChangesAsync();

        return true;
    }
}
