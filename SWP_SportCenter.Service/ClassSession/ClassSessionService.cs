using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.ClassSession;

public class ClassSessionService : IClassSessionService
{
    private readonly AppDbContext _context;

    public ClassSessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response.ClassSessionResponse>> GetAllAsync()
    {
        return await _context.ClassSessions
            .AsNoTracking()
            .Include(cs => cs.Class)
            .Include(cs => cs.Room)
            .Select(cs => new Response.ClassSessionResponse
            {
                Id = cs.Id,
                ClassId = cs.ClassId,
                ClassName = cs.Class != null ? cs.Class.ClassName : string.Empty,
                RoomId = cs.RoomId,
                RoomName = cs.Room != null ? cs.Room.RoomName : string.Empty,
                Date = cs.Date,
                StartTime = cs.StartTime,
                EndTime = cs.EndTime
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<Response.ClassSessionResponse>> GetByClassIdAsync(Guid classId)
    {
        return await _context.ClassSessions
            .AsNoTracking()
            .Include(cs => cs.Class)
            .Include(cs => cs.Room)
            .Where(cs => cs.ClassId == classId)
            .OrderBy(cs => cs.Date).ThenBy(cs => cs.StartTime)
            .Select(cs => new Response.ClassSessionResponse
            {
                Id = cs.Id,
                ClassId = cs.ClassId,
                ClassName = cs.Class != null ? cs.Class.ClassName : string.Empty,
                RoomId = cs.RoomId,
                RoomName = cs.Room != null ? cs.Room.RoomName : string.Empty,
                Date = cs.Date,
                StartTime = cs.StartTime,
                EndTime = cs.EndTime
            })
            .ToListAsync();
    }

    public async Task<Response.ClassSessionResponse?> GetByIdAsync(Guid id)
    {
        var cs = await _context.ClassSessions
            .AsNoTracking()
            .Include(x => x.Class)
            .Include(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (cs == null) return null;

        return new Response.ClassSessionResponse
        {
            Id = cs.Id,
            ClassId = cs.ClassId,
            ClassName = cs.Class != null ? cs.Class.ClassName : string.Empty,
            RoomId = cs.RoomId,
            RoomName = cs.Room != null ? cs.Room.RoomName : string.Empty,
            Date = cs.Date,
            StartTime = cs.StartTime,
            EndTime = cs.EndTime
        };
    }

    public async Task<Response.ClassSessionResponse> CreateAsync(Request.CreateClassSessionRequest request)
    {
        await ValidateSessionLogicAsync(request.ClassId, request.RoomId, request.Date, request.StartTime, request.EndTime);

        var newSession = new Repository.Entity.ClassSession
        {
            ClassId = request.ClassId,
            RoomId = request.RoomId,
            Date = request.Date.Date, // Bỏ phần giờ, chỉ lấy phần ngày
            StartTime = request.StartTime,
            EndTime = request.EndTime
        };

        _context.ClassSessions.Add(newSession);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(newSession.Id) ?? throw new Exception("Lỗi khi tạo lịch học.");
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateClassSessionRequest request)
    {
        var session = await _context.ClassSessions.FindAsync(id);
        if (session == null) return false;

        await ValidateSessionLogicAsync(request.ClassId, request.RoomId, request.Date, request.StartTime, request.EndTime, id);

        session.ClassId = request.ClassId;
        session.RoomId = request.RoomId;
        session.Date = request.Date.Date;
        session.StartTime = request.StartTime;
        session.EndTime = request.EndTime;

        _context.ClassSessions.Update(session);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var session = await _context.ClassSessions.FindAsync(id);
        if (session == null) return false;

        // Kiểm tra xem buổi học này đã có ai điểm danh chưa (Flow 2 Attendance logic)
        var hasAttendance = await _context.Attendances.AnyAsync(a => a.SessionId == id);
        if (hasAttendance)
        {
            throw new Exception("Không thể xóa buổi học này vì đã có dữ liệu điểm danh.");
        }

        _context.ClassSessions.Remove(session);
        await _context.SaveChangesAsync();

        return true;
    }

    // ==========================================
    // HÀM HỖ TRỢ KIỂM TRA RÀNG BUỘC NGHIỆP VỤ
    // ==========================================
    private async Task ValidateSessionLogicAsync(Guid classId, Guid roomId, DateTime date, TimeSpan startTime, TimeSpan endTime, Guid? currentSessionId = null)
    {
        if (startTime >= endTime)
            throw new Exception("Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");

        // 1. Kiểm tra Lớp học có tồn tại và ngày học có nằm trong giai đoạn mở lớp không
        var classEntity = await _context.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId);
        if (classEntity == null) throw new Exception("Lớp học không tồn tại.");

        if (date.Date < classEntity.StartDate.Date || date.Date > classEntity.EndDate.Date)
            throw new Exception($"Ngày xếp lịch phải nằm trong khoảng thời gian diễn ra lớp học ({classEntity.StartDate:dd/MM/yyyy} - {classEntity.EndDate:dd/MM/yyyy}).");

        // 2. Kiểm tra Phòng tập có khả dụng không (không bị trùng lịch với lớp khác)
        var conflictingSession = await _context.ClassSessions
            .AsNoTracking()
            .Where(cs => cs.RoomId == roomId && cs.Date.Date == date.Date)
            .Where(cs => currentSessionId == null || cs.Id != currentSessionId) // Bỏ qua chính nó khi Update
            .FirstOrDefaultAsync(cs => (startTime >= cs.StartTime && startTime < cs.EndTime) || 
                                       (endTime > cs.StartTime && endTime <= cs.EndTime) || 
                                       (startTime <= cs.StartTime && endTime >= cs.EndTime));
                                       
        if (conflictingSession != null)
        {
            throw new Exception($"Phòng tập đã có lịch sử dụng từ {conflictingSession.StartTime:hh\\:mm} đến {conflictingSession.EndTime:hh\\:mm} vào ngày này.");
        }
    }
}