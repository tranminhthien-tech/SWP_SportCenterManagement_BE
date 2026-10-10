using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.ClassSession;

public class ClassSessionService : IClassSessionService
{
    private readonly AppDbContext _context;

    public ClassSessionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Response.ClassSessionResponse>> GetAllAsync()
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
                Date = cs.Date.ToDateTime(TimeOnly.MinValue),
                StartTime = cs.StartTime,
                EndTime = cs.EndTime
            })
            .ToListAsync();
    }

    public async Task<List<Response.ClassSessionResponse>> GetByClassIdAsync(Guid classId)
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
                Date = cs.Date.ToDateTime(TimeOnly.MinValue),
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
            Date = cs.Date.ToDateTime(TimeOnly.MinValue),
            StartTime = cs.StartTime,
            EndTime = cs.EndTime
        };
    }

    public async Task<Response.ClassSessionResponse> CreateAsync(Request.CreateClassSessionRequest request)
    {
        await ValidateSessionLogicAsync(request.ClassId, request.RoomId, DateOnly.FromDateTime(request.Date), request.StartTime, request.EndTime);

        var newSession = new Repository.Entity.ClassSession
        {
            ClassId = request.ClassId,
            RoomId = request.RoomId,
            Date = DateOnly.FromDateTime(request.Date), // Bỏ phần giờ, chỉ lấy phần ngày
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

        await ValidateSessionLogicAsync(request.ClassId, request.RoomId, DateOnly.FromDateTime(request.Date), request.StartTime, request.EndTime, id);

        session.ClassId = request.ClassId;
        session.RoomId = request.RoomId;
        session.Date = DateOnly.FromDateTime(request.Date);
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
    //    private async Task ValidateSessionLogicAsync(
    //     Guid classId,
    //     Guid roomId,
    //     DateTime date,
    //     TimeSpan startTime,
    //     TimeSpan endTime,
    //     Guid? currentSessionId = null)
    // {
    //     if (startTime >= endTime)
    //         throw new Exception(
    //             "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
    //
    //     // 1. Kiểm tra Lớp học có tồn tại
    //     // và ngày học có nằm trong giai đoạn mở lớp không
    //     var classEntity = await _context.Classes
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(c => c.Id == classId);
    //
    //     if (classEntity == null)
    //         throw new Exception("Lớp học không tồn tại.");
    //
    //     if (date.Date < classEntity.StartDate.Date ||
    //         date.Date > classEntity.EndDate.Date)
    //     {
    //         throw new Exception(
    //             $"Ngày xếp lịch phải nằm trong khoảng thời gian diễn ra lớp học " +
    //             $"({classEntity.StartDate:dd/MM/yyyy} - " +
    //             $"{classEntity.EndDate:dd/MM/yyyy}).");
    //     }
    //
    //     // 2. Kiểm tra Phòng tập có tồn tại không
    //     var room = await _context.Rooms
    //         .AsNoTracking()
    //         .FirstOrDefaultAsync(r => r.Id == roomId);
    //
    //     if (room == null)
    //         throw new Exception("Phòng tập không tồn tại.");
    //
    //     // 3. Kiểm tra Phòng tập có đang khả dụng không
    //     if (room.Status != RoomStatus.Available)
    //     {
    //         throw new Exception(
    //             $"Phòng tập \"{room.RoomName}\" đang tạm ngưng sử dụng.");
    //     }
    //
    //     // 4. Xác định khoảng thời gian của ngày cần kiểm tra
    //     var dayStart = DateTime.SpecifyKind(
    //         date.Date,
    //         DateTimeKind.Utc);
    //
    //     var dayEnd = dayStart.AddDays(1);
    //
    //     // 5. Kiểm tra phòng có bị trùng lịch không
    //     var conflictingSession = await _context.ClassSessions
    //         .AsNoTracking()
    //         .Where(cs =>
    //             cs.RoomId == roomId &&
    //             cs.Date >= dayStart &&
    //             cs.Date < dayEnd)
    //         .Where(cs =>
    //             currentSessionId == null ||
    //             cs.Id != currentSessionId)
    //         .FirstOrDefaultAsync(cs =>
    //             (startTime >= cs.StartTime &&
    //              startTime < cs.EndTime) ||
    //
    //             (endTime > cs.StartTime &&
    //              endTime <= cs.EndTime) ||
    //
    //             (startTime <= cs.StartTime &&
    //              endTime >= cs.EndTime));
    //
    //     if (conflictingSession != null)
    //     {
    //         throw new Exception(
    //             $"Phòng tập đã có lịch sử dụng từ " +
    //             $"{conflictingSession.StartTime:hh\\:mm} đến " +
    //             $"{conflictingSession.EndTime:hh\\:mm} vào ngày này.");
    //     }
    // }
    private async Task ValidateSessionLogicAsync(
    Guid classId,
    Guid roomId,
    DateOnly date,
    TimeSpan startTime,
    TimeSpan endTime,
    Guid? currentSessionId = null)
{
    // 1. Kiểm tra thời gian
    if (startTime >= endTime)
    {
        throw new Exception(
            "Thời gian kết thúc phải lớn hơn thời gian bắt đầu.");
    }

    // 2. Kiểm tra lớp học có tồn tại không
    var classEntity = await _context.Classes
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == classId);

    if (classEntity == null)
    {
        throw new Exception("Lớp học không tồn tại.");
    }

    // 3. Kiểm tra ngày học nằm trong thời gian mở lớp
    var classStartDate = DateOnly.FromDateTime(classEntity.StartDate);
    var classEndDate = DateOnly.FromDateTime(classEntity.EndDate);

    if (date < classStartDate || date > classEndDate)
    {
        throw new Exception(
            $"Ngày xếp lịch phải nằm trong khoảng thời gian diễn ra lớp học " +
            $"({classEntity.StartDate:dd/MM/yyyy} - " +
            $"{classEntity.EndDate:dd/MM/yyyy}).");
    }

    // 4. Kiểm tra phòng tập có tồn tại không
    var room = await _context.Rooms
        .AsNoTracking()
        .FirstOrDefaultAsync(r => r.Id == roomId);

    if (room == null)
    {
        throw new Exception("Phòng tập không tồn tại.");
    }

    // 5. Kiểm tra trạng thái phòng
    if (room.Status != RoomStatus.Available)
    {
        throw new Exception(
            $"Phòng tập \"{room.RoomName}\" đang tạm ngưng sử dụng.");
    }

    // 6. Kiểm tra lịch trùng phòng trong cùng ngày
    var conflictingSession = await _context.ClassSessions
        .AsNoTracking()
        .Where(cs =>
            cs.RoomId == roomId &&
            cs.Date == date &&
            (currentSessionId == null ||
             cs.Id != currentSessionId))
        .FirstOrDefaultAsync(cs =>
            startTime < cs.EndTime &&
            endTime > cs.StartTime);

    // 7. Báo lỗi nếu thời gian bị trùng
    if (conflictingSession != null)
    {
        throw new Exception(
            $"Phòng tập \"{room.RoomName}\" đã có lịch sử dụng từ " +
            $"{conflictingSession.StartTime:hh\\:mm} đến " +
            $"{conflictingSession.EndTime:hh\\:mm} vào ngày này.");
    }
}
}