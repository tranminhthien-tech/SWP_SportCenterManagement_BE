using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using static SWP_SportCenter.Service.Room.Request;
using static SWP_SportCenter.Service.Room.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Room;

public class RoomService : IRoomService
{
    private readonly AppDbContext _context;

    public RoomService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RoomResponse>> GetAllAsync()
    {
        return await _context.Rooms
            .AsNoTracking()
            .Select(r => new RoomResponse
            {
                Id = r.Id,
                RoomName = r.RoomName,
                Capacity = r.Capacity,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<RoomResponse?> GetByIdAsync(Guid id)
    {
        var room = await _context.Rooms
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null) return null;

        return new RoomResponse
        {
            Id = room.Id,
            RoomName = room.RoomName,
            Capacity = room.Capacity,
            Status = room.Status,
            CreatedAt = room.CreatedAt,
            UpdatedAt = room.UpdatedAt
        };
    }

    public async Task<RoomResponse> CreateAsync(CreateRoomRequest request)
    {
        // Kiểm tra xem tên phòng đã tồn tại chưa
        var isExist = await _context.Rooms.AnyAsync(r => r.RoomName.ToLower() == request.RoomName.ToLower());
        if (isExist)
        {
            throw new Exception("Tên phòng tập này đã tồn tại trong hệ thống.");
        }

        var newRoom = new Repository.Entity.Room
        {
            RoomName = request.RoomName,
            Capacity = request.Capacity,
            Status = request.Status
        };

        _context.Rooms.Add(newRoom);
        await _context.SaveChangesAsync();

        return new RoomResponse
        {
            Id = newRoom.Id,
            RoomName = newRoom.RoomName,
            Capacity = newRoom.Capacity,
            Status = newRoom.Status,
            CreatedAt = newRoom.CreatedAt,
            UpdatedAt = newRoom.UpdatedAt
        };
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateRoomRequest request)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return false;

        // Kiểm tra trùng tên phòng nếu đổi tên khác
        if (room.RoomName != request.RoomName)
        {
            var isExist = await _context.Rooms.AnyAsync(r => r.RoomName.ToLower() == request.RoomName.ToLower());
            if (isExist) throw new Exception("Tên phòng tập này đã tồn tại trong hệ thống.");
        }

        room.RoomName = request.RoomName;
        room.Capacity = request.Capacity;
        room.Status = request.Status;
        room.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Rooms.Update(room);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return false;

        // Kiểm tra xem phòng này đã được xếp lịch học (ClassSession) nào chưa để tránh lỗi Restrict
        var hasSessions = await _context.ClassSessions.AnyAsync(s => s.RoomId == id);
        if (hasSessions)
        {
            throw new Exception("Không thể xóa phòng này vì đã có lịch buổi học được xếp trong phòng.");
        }

        _context.Rooms.Remove(room);
        await _context.SaveChangesAsync();

        return true;
    }
}