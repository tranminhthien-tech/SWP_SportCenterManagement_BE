using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SWP_SportCenter.Repository.Enum;

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
                Email = c.Account.Email,
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

    public async Task<Response.CoachResponse> CreateAsync(
    Request.CreateCoachRequest request)
{
    // Lấy Account
    var account = await _context.Accounts
        .FirstOrDefaultAsync(a => a.Id == request.AccountId);

    if (account == null)
        throw new Exception("Tài khoản (Account) không tồn tại.");

    // Kiểm tra Role
    if (account.Role != AccountRole.Coach)
        throw new Exception(
            "Tài khoản này không có vai trò Huấn luyện viên (Coach).");

    // Kiểm tra Account đã có profile Coach chưa
    var isProfileExist = await _context.Coaches
        .AnyAsync(c => c.AccountId == request.AccountId);

    if (isProfileExist)
        throw new Exception(
            "Tài khoản này đã có hồ sơ Huấn luyện viên.");

    // Kiểm tra số điện thoại
    var isPhoneExist = await _context.Coaches
        .AnyAsync(c => c.Phone == request.Phone);

    if (isPhoneExist)
        throw new Exception(
            "Số điện thoại này đã được sử dụng.");

    // Không kiểm tra Coach.Email nữa.
    // Email được quản lý duy nhất ở Account.
    var isEmailExist = await _context.Accounts
        .AnyAsync(a =>
            a.Email == request.Email &&
            a.Id != request.AccountId);

    if (isEmailExist)
        throw new Exception(
            "Email này đã được sử dụng.");

    // Nếu request.Email khác Email hiện tại của Account
    // thì cập nhật Email của Account.
    account.Email = request.Email;

    // Tạo Coach profile
    var newCoach = new Repository.Entity.Coach
    {
        AccountId = request.AccountId,
        FullName = request.FullName,
        Phone = request.Phone,
        Avatar = request.Avatar ?? string.Empty,
        Specialization = request.Specialization ?? string.Empty,
        ExperienceYears = request.ExperienceYears
    };

    _context.Coaches.Add(newCoach);

    await _context.SaveChangesAsync();

    // MapToResponse sẽ lấy Email từ newCoach.Account.Email
    newCoach.Account = account;

    return MapToResponse(newCoach);
}

    public async Task<bool> UpdateAsync(Guid id, Request.UpdateCoachRequest request)
    {
        // Lấy Coach kèm Account để cập nhật Email trong bảng Account
        var coach = await _context.Coaches
            .Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (coach == null)
            return false;

        // Kiểm tra số điện thoại trùng với Coach khác
        if (coach.Phone != request.Phone &&
            await _context.Coaches.AnyAsync(c =>
                c.Phone == request.Phone &&
                c.Id != id))
        {
            throw new Exception("Số điện thoại này đã được sử dụng bởi người khác.");
        }

        // Kiểm tra Email trùng với Account khác
        if (coach.Account.Email != request.Email &&
            await _context.Accounts.AnyAsync(a =>
                a.Email == request.Email &&
                a.Id != coach.AccountId))
        {
            throw new Exception("Email này đã được sử dụng bởi người khác.");
        }

        // Cập nhật thông tin Coach
        coach.FullName = request.FullName;
        coach.Phone = request.Phone;
        coach.Avatar = request.Avatar ?? string.Empty;
        coach.Specialization = request.Specialization ?? string.Empty;
        coach.ExperienceYears = request.ExperienceYears;

        // Email nằm ở Account
        coach.Account.Email = request.Email;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var coach = await _context.Coaches.FindAsync(id);
        if (coach == null) return false;

        var hasClasses = await _context.Classes.AnyAsync(c => c.CoachId == id);
        if (hasClasses) throw new Exception("Không thể xóa Huấn luyện viên này vì họ đang phụ trách các lớp học.");
        var hasTrainingData =
            await _context.TrainingPlans.AnyAsync(p => p.CoachId == id)
            || await _context.TrainingResults.AnyAsync(r => r.CoachId == id);

        if (hasTrainingData)
        {
            throw new Exception(
                "Không thể xóa Huấn luyện viên này vì đã có kế hoạch/kết quả tập luyện.");
        }
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
            .Where(cb => cb.Class != null && cb.Class.CoachId == coachId)
            .OrderBy(cb => cb.Class!.ClassName)
            .ThenBy(cb => cb.Member!.FullName)
            .Select(cb => new Response.CoachMemberResponse
            {
                MemberId = cb.MemberId,
                FullName = cb.Member != null
                    ? cb.Member.FullName
                    : string.Empty,

                Phone = cb.Member != null
                    ? cb.Member.Phone
                    : string.Empty,

                // Email lấy từ Account
                Email = cb.Member != null
                    ? cb.Member.Account.Email
                    : string.Empty,

                ClassId = cb.ClassId,

                ClassName = cb.Class != null
                    ? cb.Class.ClassName
                    : string.Empty
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
            Email = coach.Account.Email,
            Avatar = coach.Avatar,
            Specialization = coach.Specialization,
            ExperienceYears = coach.ExperienceYears
        };
    }
}