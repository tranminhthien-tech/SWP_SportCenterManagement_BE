using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Attendance;

public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly SWP_SportCenter.Service.Security.CurrentActor _actor;

    // CODE CŨ: constructor chỉ nhận DbContext.
    // FIX B15: RecordedBy được lấy tại server.
    public AttendanceService(AppDbContext context, SWP_SportCenter.Service.Security.CurrentActor actor)
    {
        _context = context;
        _actor = actor;
    }

    public async Task<IEnumerable<Response.AttendanceResponse>> GetBySessionIdAsync(Guid sessionId)
    {
        return await _context.Attendances
            .AsNoTracking()
            .Include(a => a.Session)
            .Include(a => a.Member)
            .Where(a => a.SessionId == sessionId)
            .OrderBy(a => a.Member != null ? a.Member.FullName : string.Empty)
            .Select(a => new Response.AttendanceResponse
            {
                Id = a.Id,
                SessionId = a.SessionId,
                // FIX B5: dữ liệu lớp đi kèm điểm danh.
                ClassId = a.Session.ClassId,
                ClassName = a.Session.Class.ClassName,
                SessionDate = a.Session != null
                    ? a.Session.Date.ToDateTime(TimeOnly.MinValue)
                    : DateTime.MinValue,
                SessionStartTime = a.Session != null ? a.Session.StartTime : TimeSpan.Zero,
                MemberId = a.MemberId,
                MemberName = a.Member != null ? a.Member.FullName : string.Empty,
                RecordedBy = a.RecordedBy,
                CheckInTime = a.CheckInTime,
                Status = a.Status
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<Response.AttendanceResponse>> GetByMemberIdAsync(Guid memberId)
    {
        // FIX B5: chặn Member xem lịch sử của người khác.
        await _actor.EnsureCanViewMemberAsync(memberId);
        return await _context.Attendances
            .AsNoTracking()
            .Include(a => a.Session)
            .Include(a => a.Member)
            .Where(a => a.MemberId == memberId)
            .OrderByDescending(a =>
                a.Session != null ? a.Session.Date : DateOnly.MinValue)
            .Select(a => new Response.AttendanceResponse
            {
                Id = a.Id,
                SessionId = a.SessionId,
                // FIX B5: dữ liệu lớp đi kèm điểm danh.
                ClassId = a.Session.ClassId,
                ClassName = a.Session.Class.ClassName,
                SessionDate = a.Session != null
                    ? a.Session.Date.ToDateTime(TimeOnly.MinValue)
                    : DateTime.MinValue,
                SessionStartTime = a.Session != null ? a.Session.StartTime : TimeSpan.Zero,
                MemberId = a.MemberId,
                MemberName = a.Member != null ? a.Member.FullName : string.Empty,
                RecordedBy = a.RecordedBy,
                CheckInTime = a.CheckInTime,
                Status = a.Status
            })
            .ToListAsync();
    }

    public async Task<Response.AttendanceResponse> RecordAttendanceAsync(Request.RecordAttendanceRequest request)
    {
        // 1. Kiểm tra session có tồn tại không
        // FIX 23/10: kiểm tra ownership trước mọi thao tác ghi.
        await EnsureCoachOwnsSessionAsync(request.SessionId);
        var session = await _context.ClassSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);
            
        // CODE CŨ: if (session == null) throw new Exception("Buổi học không tồn tại.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        if (session == null) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Buổi học không tồn tại.");

        // 2. Kiểm tra học viên có đăng ký tham gia lớp này không
        var isBooked = await _context.ClassBookings
            .AnyAsync(cb => cb.ClassId == session.ClassId && cb.MemberId == request.MemberId);
            
        // CODE CŨ: if (!isBooked) throw new Exception("Học viên này không có trong danh sách đăng ký của lớp học.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        if (!isBooked) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Học viên này không có trong danh sách đăng ký của lớp học.");

        // 3. Kiểm tra xem học viên này đã được điểm danh trong buổi này chưa
        var existingAttendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.SessionId == request.SessionId && a.MemberId == request.MemberId);
        
        // CODE CŨ: if (existingAttendance != null) throw new Exception("Học viên này đã được điểm danh trong buổi học này rồi.");
        // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
        if (existingAttendance != null) throw new SWP_SportCenter.Service.Exceptions.BusinessException("Học viên này đã được điểm danh trong buổi học này rồi.");

        var newAttendance = new Repository.Entity.Attendance
        {
            SessionId = request.SessionId,
            MemberId = request.MemberId,
            // CODE CŨ: RecordedBy = request.RecordedBy,
            // FIX B15: bỏ qua người ghi do client truyền.
            RecordedBy = _actor.AccountId,
            CheckInTime = DateTimeOffset.UtcNow,
            Status = request.Status
        };

        _context.Attendances.Add(newAttendance);
        await _context.SaveChangesAsync();
        
        await SyncBookingStatusAsync(request.MemberId, session.ClassId);

        var savedAttendance = await _context.Attendances
            .Include(a => a.Member)
            // CODE CŨ: .Include(a => a.Session)
            // FIX B5: mapping ClassName sau khi ghi cần tải cả lớp của session.
            .Include(a => a.Session).ThenInclude(s => s.Class)
            .FirstAsync(a => a.Id == newAttendance.Id);

        return new Response.AttendanceResponse
        {
            Id = savedAttendance.Id,
            SessionId = savedAttendance.SessionId,
            // FIX B5: dữ liệu lớp đi kèm điểm danh.
            ClassId = savedAttendance.Session.ClassId,
            ClassName = savedAttendance.Session.Class.ClassName,
            SessionDate = savedAttendance.Session.Date.ToDateTime(TimeOnly.MinValue),
            SessionStartTime = savedAttendance.Session.StartTime,
            MemberId = savedAttendance.MemberId,
            MemberName = savedAttendance.Member.FullName,
            RecordedBy = savedAttendance.RecordedBy,
            CheckInTime = savedAttendance.CheckInTime,
            Status = savedAttendance.Status
        };
    }

    public async Task<bool> UpdateAttendanceAsync(Guid id, Request.UpdateAttendanceRequest request)
    {
        var attendance = await _context.Attendances.FindAsync(id);
        if (attendance == null) return false;

        // CODE CŨ: cập nhật ngay sau khi tìm thấy Attendance, không kiểm tra người dạy.
        // FIX 23/10: Coach không được sửa điểm danh lớp của người khác.
        await EnsureCoachOwnsSessionAsync(attendance.SessionId);

        attendance.Status = request.Status;
        // CODE CŨ: attendance.RecordedBy = request.RecordedBy;
        // FIX B15: ghi danh tính người thực hiện cập nhật.
        attendance.RecordedBy = _actor.AccountId;
        
        _context.Attendances.Update(attendance);
        await _context.SaveChangesAsync();
        
        var session = await _context.ClassSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == attendance.SessionId);

        if (session != null)
        {
            await SyncBookingStatusAsync(
                attendance.MemberId,
                session.ClassId);
        }

        return true;
    }
    // FIX 23/10: lấy Coach từ token, không tin CoachId/RecordedBy do client gửi.
    private async Task EnsureCoachOwnsSessionAsync(Guid sessionId)
    {
        if (!_actor.IsCoach || !await _context.ClassSessions.AnyAsync(s =>
                s.Id == sessionId && !s.IsDeleted && !s.Class.IsDeleted &&
                !s.Class.Coach.IsDeleted && s.Class.Coach.AccountId == _actor.AccountId))
            throw new UnauthorizedAccessException("Bạn chỉ được điểm danh lớp do mình phụ trách.");
    }

    private async Task SyncBookingStatusAsync(Guid memberId, Guid classId)
    {
        var booking = await _context.ClassBookings
            .FirstOrDefaultAsync(cb =>
                cb.MemberId == memberId &&
                cb.ClassId == classId);

        if (booking == null ||
            booking.Status == BookingStatus.Cancelled)
            return;

        var hasAttended = await _context.Attendances
            .AnyAsync(a =>
                a.MemberId == memberId &&
                a.Session.ClassId == classId &&
                (a.Status == AttendanceStatus.Present ||
                 a.Status == AttendanceStatus.Late));

        var newStatus = hasAttended
            ? BookingStatus.Attended
            : BookingStatus.Booked;

        if (booking.Status != newStatus)
        {
            booking.Status = newStatus;
            await _context.SaveChangesAsync();
        }
    }
}
