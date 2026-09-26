using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;

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
                x.Email.Contains(searchTerm));
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
                Email = x.Email,
                Avatar = x.Avatar,
                TrainingGoal = x.TrainingGoal
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.MemberResponse>
        {
            Items = items,
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
                Email = x.Email,
                Avatar = x.Avatar,
                TrainingGoal = x.TrainingGoal
            })
            .FirstOrDefaultAsync();
    }

    // POST /api/members
    public async Task<Response.MemberResponse> CreateAsync(
        Request.MemberRequest request)
    {
        var member = new Repository.Entity.Member
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Dob = request.Dob,
            Gender = request.Gender,
            Phone = request.Phone,
            Email = request.Email,
            Avatar = request.Avatar,
            TrainingGoal = request.TrainingGoal,
            CreatedAt = DateTimeOffset.UtcNow
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
            Email = member.Email,
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
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (member == null)
        {
            return false;
        }
        
        member.FullName = request.FullName;
        member.Dob = request.Dob;
        member.Gender = request.Gender;
        member.Phone = request.Phone;
        member.Email = request.Email;
        member.Avatar = request.Avatar;
        member.TrainingGoal = request.TrainingGoal;
        member.UpdatedAt = DateTimeOffset.UtcNow;

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
                Email = x.Email,
                Avatar = x.Avatar,
                TrainingGoal = x.TrainingGoal
            })
            .FirstOrDefaultAsync();
    }

    // PUT /api/members/me
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
            .FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                !x.IsDeleted);

        if (member == null)
        {
            return false;
        }

        member.FullName = request.FullName;
        member.Dob = request.Dob;
        member.Gender = request.Gender;
        member.Phone = request.Phone;
        member.Email = request.Email;
        member.Avatar = request.Avatar;
        member.TrainingGoal = request.TrainingGoal;
        member.UpdatedAt = DateTimeOffset.UtcNow;

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
                Email = x.Email,
                Avatar = x.Avatar,
                TrainingGoal = x.TrainingGoal
            })
            .FirstOrDefaultAsync();
    }
}