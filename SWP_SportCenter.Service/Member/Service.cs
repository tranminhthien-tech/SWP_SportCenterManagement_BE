using Microsoft.AspNetCore.Http;
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
                 x.Phone.Contains(searchTerm) ||
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
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId);

        if (account == null)
        {
            throw new InvalidOperationException("Tài khoản không tồn tại.");
        }

        if (account.Role != AccountRole.Member)
        {
            throw new InvalidOperationException(
                "Tài khoản này không có vai trò Member.");
        }

        var isMemberExist = await _dbContext.Members
            .AnyAsync(m => m.AccountId == request.AccountId);

        if (isMemberExist)
        {
            throw new InvalidOperationException(
                "Tài khoản này đã có hồ sơ Member.");
        }

        var isPhoneExist = await _dbContext.Members
            .AnyAsync(m => m.Phone == request.Phone);

        if (isPhoneExist)
        {
            throw new InvalidOperationException(
                "Số điện thoại này đã được sử dụng.");
        }

        var member = new Repository.Entity.Member
        {
            Id = Guid.NewGuid(),
            AccountId = request.AccountId,
            FullName = request.FullName.Trim(),
            Dob = request.Dob,
            Gender = request.Gender,
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
            throw new InvalidOperationException(
                "Số điện thoại này đã được sử dụng.");
        }

        // Kiểm tra Email trùng Account khác
        var isEmailExist = await _dbContext.Accounts
            .AnyAsync(x =>
                x.Email == request.Email &&
                x.Id != member.AccountId);

        if (isEmailExist)
        {
            throw new InvalidOperationException(
                "Email này đã được sử dụng.");
        }

        member.FullName = request.FullName.Trim();
        member.Dob = request.Dob;
        member.Gender = request.Gender;
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
            .FindFirst("UserId")?.Value;

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
        var userIdClaim = _httpContext.HttpContext?
            .User
            .FindFirst("UserId")?.Value;

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
            throw new InvalidOperationException(
                "Số điện thoại này đã được sử dụng.");
        }

        // Kiểm tra Email đã được Account khác sử dụng chưa
        var isEmailExist = await _dbContext.Accounts
            .AnyAsync(x =>
                x.Email == request.Email &&
                x.Id != accountId);

        if (isEmailExist)
        {
            throw new InvalidOperationException(
                "Email này đã được sử dụng.");
        }

        member.FullName = request.FullName.Trim();
        member.Dob = request.Dob;
        member.Gender = request.Gender;
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