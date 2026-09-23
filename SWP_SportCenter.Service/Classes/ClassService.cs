using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Classes;

public class ClassService : IClassService
{
    private readonly AppDbContext _context;

    public ClassService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response.ClassResponse>> GetAllAsync()
    {
        // Include Category và Coach để lấy được Tên hiển thị lên giao diện
        return await _context.Classes
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Coach)
            .Select(c => new Response.ClassResponse
            {
                Id = c.Id,
                ClassName = c.ClassName,
                CategoryId = c.CategoryId,
                CategoryName = c.Category != null ? c.Category.CategoryName : string.Empty,
                CoachId = c.CoachId,
                CoachName = c.Coach != null ? c.Coach.FullName : string.Empty,
                MaxCapacity = c.MaxCapacity,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<Response.ClassResponse?> GetByIdAsync(Guid id)
    {
        var classEntity = await _context.Classes
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.Coach)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (classEntity == null) return null;

        return new Response.ClassResponse
        {
            Id = classEntity.Id,
            ClassName = classEntity.ClassName,
            CategoryId = classEntity.CategoryId,
            CategoryName = classEntity.Category != null ? classEntity.Category.CategoryName : string.Empty,
            CoachId = classEntity.CoachId,
            CoachName = classEntity.Coach != null ? classEntity.Coach.FullName : string.Empty,
            MaxCapacity = classEntity.MaxCapacity,
            StartDate = classEntity.StartDate,
            EndDate = classEntity.EndDate,
            Status = classEntity.Status,
            CreatedAt = classEntity.CreatedAt,
            UpdatedAt = classEntity.UpdatedAt
        };
    }

    public async Task<Response.ClassResponse> CreateAsync(Request.CreateClassRequest request)
    {
        // Validate xem StartDate có trước EndDate không
        if (request.StartDate >= request.EndDate)
        {
            throw new Exception("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }

        var newClass = new Class
        {
            ClassName = request.ClassName,
            CategoryId = request.CategoryId,
            CoachId = request.CoachId,
            MaxCapacity = request.MaxCapacity,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status
        };

        _context.Classes.Add(newClass);
        await _context.SaveChangesAsync();

        // Query lại để lấy được thông tin CategoryName và CoachName
        return await GetByIdAsync(newClass.Id) ?? throw new Exception("Lỗi khi tạo lớp học.");
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateClassRequest request)
    {
        if (request.StartDate >= request.EndDate)
        {
            throw new Exception("Ngày kết thúc phải lớn hơn ngày bắt đầu.");
        }

        var classEntity = await _context.Classes.FindAsync(id);
        if (classEntity == null) return false;

        classEntity.ClassName = request.ClassName;
        classEntity.CategoryId = request.CategoryId;
        classEntity.CoachId = request.CoachId;
        classEntity.MaxCapacity = request.MaxCapacity;
        classEntity.StartDate = request.StartDate;
        classEntity.EndDate = request.EndDate;
        classEntity.Status = request.Status;
        classEntity.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Classes.Update(classEntity);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var classEntity = await _context.Classes.FindAsync(id);
        if (classEntity == null) return false;

        // Kiểm tra xem Lớp này đã được xếp lịch hoặc có học viên đăng ký chưa
        var hasSessions = await _context.ClassSessions.AnyAsync(cs => cs.ClassId == id);
        var hasBookings = await _context.ClassBookings.AnyAsync(cb => cb.ClassId == id);

        if (hasSessions || hasBookings)
        {
            throw new Exception("Không thể xóa lớp học này vì đã có lịch học được xếp hoặc đã có học viên đăng ký.");
        }

        _context.Classes.Remove(classEntity);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<Response.ClassMemberResponse>> GetMembersByClassIdAsync(Guid classId)
    {
        // Join với bảng Member thông qua ClassBooking để lấy danh sách học viên
        return await _context.ClassBookings
            .AsNoTracking()
            .Include(cb => cb.Member)
            .Where(cb => cb.ClassId == classId)
            .Select(cb => new Response.ClassMemberResponse
            {
                MemberId = cb.MemberId,
                FullName = cb.Member.FullName,
                Phone = cb.Member.Phone,
                Email = cb.Member.Email,
                BookingStatus = cb.Status,
                BookingDate = cb.BookingDate
            })
            .ToListAsync();
    }
}