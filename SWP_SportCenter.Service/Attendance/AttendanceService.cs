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

    public AttendanceService(AppDbContext context)
    {
        _context = context;
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
                SessionDate = a.Session != null ? a.Session.Date : DateTime.MinValue,
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
        return await _context.Attendances
            .AsNoTracking()
            .Include(a => a.Session)
            .Include(a => a.Member)
            .Where(a => a.MemberId == memberId)
            .OrderByDescending(a => a.Session != null ? a.Session.Date : DateTime.MinValue)
            .Select(a => new Response.AttendanceResponse
            {
                Id = a.Id,
                SessionId = a.SessionId,
                SessionDate = a.Session != null ? a.Session.Date : DateTime.MinValue,
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
        var session = await _context.ClassSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SessionId);
            
        if (session == null) throw new Exception("Buổi học không tồn tại.");

        // 2. Kiểm tra học viên có đăng ký tham gia lớp này không
        var isBooked = await _context.ClassBookings
            .AnyAsync(cb => cb.ClassId == session.ClassId && cb.MemberId == request.MemberId);
            
        if (!isBooked) throw new Exception("Học viên này không có trong danh sách đăng ký của lớp học.");

        // 3. Kiểm tra xem học viên này đã được điểm danh trong buổi này chưa
        var existingAttendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.SessionId == request.SessionId && a.MemberId == request.MemberId);
        
        if (existingAttendance != null) throw new Exception("Học viên này đã được điểm danh trong buổi học này rồi.");

        var newAttendance = new Repository.Entity.Attendance
        {
            SessionId = request.SessionId,
            MemberId = request.MemberId,
            RecordedBy = request.RecordedBy,
            CheckInTime = DateTimeOffset.UtcNow,
            Status = request.Status
        };

        _context.Attendances.Add(newAttendance);
        await _context.SaveChangesAsync();

        var savedAttendance = await _context.Attendances
            .Include(a => a.Member)
            .Include(a => a.Session)
            .FirstAsync(a => a.Id == newAttendance.Id);

        return new Response.AttendanceResponse
        {
            Id = savedAttendance.Id,
            SessionId = savedAttendance.SessionId,
            SessionDate = savedAttendance.Session.Date,
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

        attendance.Status = request.Status;
        attendance.RecordedBy = request.RecordedBy; // Cập nhật lại người thao tác sửa
        
        _context.Attendances.Update(attendance);
        await _context.SaveChangesAsync();

        return true;
    }
}