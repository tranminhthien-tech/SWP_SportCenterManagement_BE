using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Coach;

public class CoachService : ICoachService
{
    private readonly AppDbContext _context;

    public CoachService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Response.CoachResponse>> GetAllAsync()
    {
        return await _context.Coaches
            .AsNoTracking()
            .Select(c => new Response.CoachResponse
            {
                Id = c.Id,
                AccountId = c.AccountId,
                FullName = c.FullName,
                Phone = c.Phone,
                Email = c.Email,
                Avatar = c.Avatar,
                Specialization = c.Specialization,
                ExperienceYears = c.ExperienceYears
            })
            .ToListAsync();
    }

    public async Task<Response.CoachResponse?> GetByIdAsync(Guid id)
    {
        var coach = await _context.Coaches.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (coach == null) return null;

        return MapToResponse(coach);
    }

    public async Task<Response.CoachResponse?> GetByAccountIdAsync(Guid accountId)
    {
        var coach = await _context.Coaches.AsNoTracking().FirstOrDefaultAsync(c => c.AccountId == accountId);
        if (coach == null) return null;

        return MapToResponse(coach);
    }

    public async Task<Response.CoachResponse> CreateAsync(Request.CreateCoachRequest request)
    {
        var isAccountExist = await _context.Accounts.AnyAsync(a => a.Id == request.AccountId);
        if (!isAccountExist) throw new Exception("Tài khoản (Account) không tồn tại.");

        var isProfileExist = await _context.Coaches.AnyAsync(c => c.AccountId == request.AccountId);
        if (isProfileExist) throw new Exception("Tài khoản này đã có hồ sơ Huấn luyện viên.");

        var isPhoneExist = await _context.Coaches.AnyAsync(c => c.Phone == request.Phone);
        if (isPhoneExist) throw new Exception("Số điện thoại này đã được sử dụng.");

        var isEmailExist = await _context.Coaches.AnyAsync(c => c.Email == request.Email);
        if (isEmailExist) throw new Exception("Email này đã được sử dụng.");

        var newCoach = new Repository.Entity.Coach
        {
            AccountId = request.AccountId,
            FullName = request.FullName,
            Phone = request.Phone,
            Email = request.Email,
            Avatar = request.Avatar,
            Specialization = request.Specialization,
            ExperienceYears = request.ExperienceYears
        };

        _context.Coaches.Add(newCoach);
        await _context.SaveChangesAsync();

        return MapToResponse(newCoach);
    }

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateCoachRequest request)
    {
        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return false;

        if (coach.Phone != request.Phone && await _context.Coaches.AnyAsync(c => c.Phone == request.Phone))
            throw new Exception("Số điện thoại này đã được sử dụng bởi người khác.");

        if (coach.Email != request.Email && await _context.Coaches.AnyAsync(c => c.Email == request.Email))
            throw new Exception("Email này đã được sử dụng bởi người khác.");

        coach.FullName = request.FullName;
        coach.Phone = request.Phone;
        coach.Email = request.Email;
        coach.Avatar = request.Avatar;
        coach.Specialization = request.Specialization;
        coach.ExperienceYears = request.ExperienceYears;

        _context.Coaches.Update(coach);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return false;

        var hasClasses = await _context.Classes.AnyAsync(c => c.CoachId == id);
        if (hasClasses) throw new Exception("Không thể xóa Huấn luyện viên này vì họ đang phụ trách các lớp học.");

        _context.Coaches.Remove(coach);
        await _context.SaveChangesAsync();
        return true;
    }

    // Nghiệp vụ Flow 2: Lấy danh sách các lớp học HLV đang phụ trách
    public async Task<IEnumerable<Response.CoachClassResponse>> GetCoachClassesAsync(Guid coachId)
    {
        return await _context.Classes
            .AsNoTracking()
            .Include(c => c.Category)
            .Include(c => c.ClassBookings)
            .Where(c => c.CoachId == coachId)
            .OrderByDescending(c => c.StartDate)
            .Select(c => new Response.CoachClassResponse
            {
                ClassId = c.Id,
                ClassName = c.ClassName,
                CategoryName = c.Category != null ? c.Category.CategoryName : string.Empty,
                MaxCapacity = c.MaxCapacity,
                CurrentEnrolled = c.ClassBookings.Count, // Đếm số học viên đã đăng ký
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status
            })
            .ToListAsync();
    }

    // Nghiệp vụ Flow 2: Lấy danh sách học viên HLV đang phụ trách
    public async Task<IEnumerable<Response.CoachMemberResponse>> GetCoachMembersAsync(Guid coachId)
    {
        return await _context.ClassBookings
            .AsNoTracking()
            .Include(cb => cb.Member)
            .Include(cb => cb.Class)
            .Where(cb => cb.Class != null && cb.Class.CoachId == coachId)
            .OrderBy(cb => cb.Class!.ClassName).ThenBy(cb => cb.Member!.FullName)
            .Select(cb => new Response.CoachMemberResponse
            {
                MemberId = cb.MemberId,
                FullName = cb.Member != null ? cb.Member.FullName : string.Empty,
                Phone = cb.Member != null ? cb.Member.Phone : string.Empty,
                Email = cb.Member != null ? cb.Member.Email : string.Empty,
                ClassId = cb.ClassId,
                ClassName = cb.Class != null ? cb.Class.ClassName : string.Empty
            })
            .ToListAsync();
    }

    private Response.CoachResponse MapToResponse(Repository.Entity.Coach coach)
    {
        return new Response.CoachResponse
        {
            Id = coach.Id,
            AccountId = coach.AccountId,
            FullName = coach.FullName,
            Phone = coach.Phone,
            Email = coach.Email,
            Avatar = coach.Avatar,
            Specialization = coach.Specialization,
            ExperienceYears = coach.ExperienceYears
        };
    }
}