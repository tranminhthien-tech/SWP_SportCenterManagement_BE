using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Member;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContext;

    public Service(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContext = httpContextAccessor;
    }
    
     // GET /api/members
     public async Task<Base.Response.PageResult<Response.MemberResponse>> GetAllAsync(
         int pageIndex,
         int pageSize,
         string? searchTerm)
     {
         var query = _dbContext.Members
             .AsNoTracking()
             .Where(x => !x.IsDeleted);

         if (!string.IsNullOrWhiteSpace(searchTerm))
         {
             searchTerm = searchTerm.Trim();

             query = query.Where(x =>
                 x.FullName.Contains(searchTerm) ||
                 // CODE CŨ: x.Phone.Contains(searchTerm)
                 // FIX B18: profile cũ cho phép Phone null.
                 (x.Phone != null && x.Phone.Contains(searchTerm)) ||
                 x.Account.Email.Contains(searchTerm));
         }

         var totalItems = await query.CountAsync();

         var items = await query
             .OrderBy(x => x.FullName)
             .Skip((pageIndex - 1) * pageSize)
             .Take(pageSize)
             .Select(x => new Response.MemberResponse
             {
                 MemberId = x.Id,
                 AccountId = x.AccountId,
                 // FIX B24: đủ thông tin cho lễ tân mà không cần quyền Accounts.
                 CreatedAt = x.Account.CreatedAt,
                 AccountStatus = x.Account.Status,
                 FullName = x.FullName,
                 Dob = x.Dob,
                 Gender = x.Gender,
                 Phone = x.Phone,
                 Email = x.Account.Email,
                 Avatar = x.Avatar,
                 TrainingGoal = x.TrainingGoal
             })
             .ToListAsync();

         return new Base.Response.PageResult<Response.MemberResponse>
         {
             Data = items,
             TotalItems = totalItems,
             PageSize = pageSize,
             PageIndex = pageIndex
         };
     }

    // GET /api/members/{id}
    public async Task<Response.MemberResponse?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Members
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new Response.MemberResponse
            {
                MemberId = x.Id,
                AccountId = x.AccountId,
                // FIX B24: bổ sung metadata Account vào hồ sơ.
                CreatedAt = x.Account.CreatedAt,
                AccountStatus = x.Account.Status,
                FullName = x.FullName,
                Dob = x.Dob,
                Gender = x.Gender,
                Phone = x.Phone,
                Email = x.Account.Email,
                Avatar = x.Avatar,
                TrainingGoal = x.TrainingGoal
            })
            .FirstOrDefaultAsync();
    }

    // POST /api/members
    public async Task<Response.MemberResponse> CreateAsync(
        Request.MemberRequest request)
    {
        // FIX B17: bảo vệ cả lời gọi Service trực tiếp.
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId && !a.IsDeleted);

        if (account == null)
        {
            // CODE CŨ: throw new Exception("Tài khoản không tồn tại.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException("Tài khoản không tồn tại.");
        }

        if (account.Role != AccountRole.Member)
        {
            // CODE CŨ: throw new Exception(
            //                 "Tài khoản này không có vai trò Member.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException(
                "Tài khoản này không có vai trò Member.");
        }

        // CODE CŨ: AnyAsync(m => m.AccountId == request.AccountId) chặn cả hồ sơ đã xóa mềm.
        // FIX B27: khôi phục đúng bản ghi cũ để giữ ID và lịch sử liên quan.
        var existingMember = await _dbContext.Members
            .FirstOrDefaultAsync(m => m.AccountId == request.AccountId);

        if (existingMember != null && !existingMember.IsDeleted)
        {
            // CODE CŨ: throw new Exception(
            //                 "Tài khoản này đã có hồ sơ Member.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException(
                "Tài khoản này đã có hồ sơ Member.");
        }

        var isPhoneExist = await _dbContext.Members
            // CODE CŨ: .AnyAsync(m => m.Phone == request.Phone);
            // FIX B27: loại chính hồ sơ đang khôi phục, vẫn tôn trọng unique phone toàn bảng.
            .AnyAsync(m => m.Phone == request.Phone.Trim() && m.AccountId != request.AccountId);

        if (isPhoneExist)
        {
            // CODE CŨ: throw new Exception(
            //                 "Số điện thoại này đã được sử dụng.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.BusinessException(
                "Số điện thoại này đã được sử dụng.");
        }

        // FIX B27: Email thuộc Account; kiểm tra trùng trước khi thay đổi bất kỳ dữ liệu nào.
        if (await _dbContext.Accounts.AnyAsync(a => a.Id != account.Id && a.Email == request.Email.Trim()))
            throw new Exceptions.ConflictException("Email này đã được sử dụng.");
        account.Email = request.Email.Trim();
        if (existingMember != null)
        {
            existingMember.IsDeleted = false;
            existingMember.FullName = request.FullName.Trim();
            existingMember.Dob = request.Dob;
            existingMember.Gender = request.Gender ?? string.Empty;
            existingMember.Phone = request.Phone.Trim();
            existingMember.Avatar = request.Avatar ?? string.Empty;
            existingMember.TrainingGoal = request.TrainingGoal ?? string.Empty;
            await _dbContext.SaveChangesAsync();
            return (await GetByIdAsync(existingMember.Id))!;
        }

        var member = new Repository.Entity.Member
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            FullName = request.FullName.Trim(),
            Dob = request.Dob,
            // CODE CŨ: Gender = request.Gender,
            // FIX B17: chuẩn hóa trường tùy chọn, không ghi null vào cột bắt buộc.
            Gender = request.Gender ?? string.Empty,
            Phone = request.Phone.Trim(),
            Avatar = request.Avatar ?? string.Empty,
            TrainingGoal = request.TrainingGoal ?? string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,

            Account = account
        };

        await _dbContext.Members.AddAsync(member);
        await _dbContext.SaveChangesAsync();

        return new Response.MemberResponse
        {
            MemberId = member.Id,
            AccountId = member.AccountId,
            // FIX B24: CreatedAt là ngày tạo Account, không đổi khi khôi phục profile.
            CreatedAt = account.CreatedAt,
            AccountStatus = account.Status,
            FullName = member.FullName,
            Dob = member.Dob,
            Gender = member.Gender,
            Phone = member.Phone,
            Email = account.Email,
            Avatar = member.Avatar,
            TrainingGoal = member.TrainingGoal
        };
    }

    // PUT /api/members/{id}
    public async Task<bool> UpdateAsync(
        Guid id,
        Request.MemberRequest request)
    {
        // FIX B17: validate trước mọi Trim và ghi database.
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        var member = await _dbContext.Members
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (member == null)
        {
            return false;
        }

        // Kiểm tra số điện thoại trùng Member khác
        var isPhoneExist = await _dbContext.Members
            .AnyAsync(x =>
                x.Phone == request.Phone &&
                x.Id != id &&
                !x.IsDeleted);

        if (isPhoneExist)
        {
            // CODE CŨ: throw new InvalidOperationException(
            //                 "Số điện thoại này đã được sử dụng.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException(
                "Số điện thoại này đã được sử dụng.");
        }

        // Kiểm tra Email trùng Account khác
        var isEmailExist = await _dbContext.Accounts
            .AnyAsync(x =>
                x.Email == request.Email &&
                x.Id != member.AccountId);

        if (isEmailExist)
        {
            // CODE CŨ: throw new InvalidOperationException(
            //                 "Email này đã được sử dụng.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException(
                "Email này đã được sử dụng.");
        }

        member.FullName = request.FullName.Trim();
        member.Dob = request.Dob;
        // CODE CŨ: member.Gender = request.Gender;
        // FIX B17: Gender là tùy chọn ở form.
        member.Gender = request.Gender ?? string.Empty;
        member.Phone = request.Phone.Trim();
        member.Avatar = request.Avatar ?? string.Empty;
        member.TrainingGoal = request.TrainingGoal ?? string.Empty;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        // Email nằm trong Account
        member.Account.Email = request.Email.Trim();

        await _dbContext.SaveChangesAsync();

        return true;
    }

    // DELETE /api/members/{id}
    public async Task<bool> DeleteAsync(Guid id)
    {
        var member = await _dbContext.Members
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (member == null)
        {
            return false;
        }

        member.IsDeleted = true;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    // GET /api/members/me
    public async Task<Response.MemberResponse?> GetMyProfileAsync()
    {
        var userIdClaim = _httpContext.HttpContext?
            .User
            // CODE CŨ: .FindFirst("UserId")?.Value;
            // FIX B2: JWT thực tế dùng ClaimTypes.NameIdentifier.
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var accountId))
        {
            return null;
        }

        return await _dbContext.Members
            .AsNoTracking()
            .Where(x =>
                x.AccountId == accountId &&
                !x.IsDeleted)
            .Select(x => new Response.MemberResponse
            {
                MemberId = x.Id,
                AccountId = x.AccountId,
                // FIX B24: bổ sung metadata Account vào hồ sơ.
                CreatedAt = x.Account.CreatedAt,
                AccountStatus = x.Account.Status,
                FullName = x.FullName,
                Dob = x.Dob,
                Gender = x.Gender,
                Phone = x.Phone,
                Email = x.Account.Email,
                Avatar = x.Avatar,
                TrainingGoal = x.TrainingGoal
            })
            .FirstOrDefaultAsync();
    }

    // PUT /api/members/me
    // public async Task<bool> UpdateMyProfileAsync(
    //     Request.MemberRequest request)
    // {
    //     var userIdClaim = _httpContext.HttpContext?
    //         .User
    //         .FindFirst("UserId")?.Value;
    //
    //     if (!Guid.TryParse(userIdClaim, out var accountId))
    //     {
    //         return false;
    //     }
    //
    //     var member = await _dbContext.Members
    //         .FirstOrDefaultAsync(x =>
    //             x.AccountId == accountId &&
    //             !x.IsDeleted);
    //
    //     if (member == null)
    //     {
    //         return false;
    //     }
    //
    //     member.FullName = request.FullName;
    //     member.Dob = request.Dob;
    //     member.Gender = request.Gender;
    //     member.Phone = request.Phone;
    //     member.Email = request.Email;
    //     member.Avatar = request.Avatar;
    //     member.TrainingGoal = request.TrainingGoal;
    //     member.UpdatedAt = DateTimeOffset.UtcNow;
    //
    //     await _dbContext.SaveChangesAsync();
    //
    //     return true;
    // }
    public async Task<bool> UpdateMyProfileAsync(
        Request.MemberRequest request)
    {
        // FIX B17: không để thiếu phone/email/dob trở thành lỗi runtime.
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        var userIdClaim = _httpContext.HttpContext?
            .User
            // CODE CŨ: .FindFirst("UserId")?.Value;
            // FIX B2: cập nhật profile theo subject có trong token.
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(userIdClaim, out var accountId))
        {
            return false;
        }

        var member = await _dbContext.Members
            .Include(x => x.Account)
            .FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                !x.IsDeleted);

        if (member == null)
        {
            return false;
        }

        // Kiểm tra số điện thoại đã được Member khác sử dụng chưa
        var isPhoneExist = await _dbContext.Members
            .AnyAsync(x =>
                x.Phone == request.Phone &&
                x.Id != member.Id &&
                !x.IsDeleted);

        if (isPhoneExist)
        {
            // CODE CŨ: throw new InvalidOperationException(
            //                 "Số điện thoại này đã được sử dụng.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException(
                "Số điện thoại này đã được sử dụng.");
        }

        // Kiểm tra Email đã được Account khác sử dụng chưa
        var isEmailExist = await _dbContext.Accounts
            .AnyAsync(x =>
                x.Email == request.Email &&
                x.Id != accountId);

        if (isEmailExist)
        {
            // CODE CŨ: throw new InvalidOperationException(
            //                 "Email này đã được sử dụng.");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException(
                "Email này đã được sử dụng.");
        }

        member.FullName = request.FullName.Trim();
        member.Dob = request.Dob;
        // CODE CŨ: member.Gender = request.Gender;
        // FIX B17: không đưa null xuống cột bắt buộc.
        member.Gender = request.Gender ?? string.Empty;
        member.Phone = request.Phone.Trim();
        member.Avatar = request.Avatar ?? string.Empty;
        member.TrainingGoal = request.TrainingGoal ?? string.Empty;
        member.UpdatedAt = DateTimeOffset.UtcNow;

        // Email nằm trong Account
        member.Account.Email = request.Email.Trim();

        await _dbContext.SaveChangesAsync();

        return true;
    }

    // GET /api/members/{id}/summary
    public async Task<Response.MemberResponse?> GetSummaryAsync(Guid id)
    {
        return await _dbContext.Members
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new Response.MemberResponse
            {
                MemberId = x.Id,
                AccountId = x.AccountId,
                // FIX B24: bổ sung metadata Account vào hồ sơ.
                CreatedAt = x.Account.CreatedAt,
                AccountStatus = x.Account.Status,
                FullName = x.FullName,
                Dob = x.Dob,
                Gender = x.Gender,
                Phone = x.Phone,
                Email = x.Account.Email,
                Avatar = x.Avatar,
                TrainingGoal = x.TrainingGoal
            })
            .FirstOrDefaultAsync();
    }
}
