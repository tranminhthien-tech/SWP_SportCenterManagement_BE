using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Membership;

public class Service: IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Service(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    // Lấy AccountId từ tài khoản đang đăng nhập
    private Guid GetCurrentAccountId()
    {
        var accountId = _httpContextAccessor.HttpContext?
            .User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(accountId, out var id))
        {
            throw new UnauthorizedAccessException(
                "Account ID not found in token");
        }

        return id;
    }

    // Lấy MemberId từ AccountId đang đăng nhập
    private async Task<Guid> GetCurrentMemberIdAsync()
    {
        var accountId = GetCurrentAccountId();

        var member = await _dbContext.Members
            .FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                !x.IsDeleted);

        if (member == null)
        {
            throw new KeyNotFoundException(
                "Member not found");
        }

        return member.Id;
    }

    // Chuyển Entity sang Response
    private static Response.MembershipResponse MapToResponse(
        Repository.Entity.Membership membership)
    {
        return new Response.MembershipResponse
        {
            MembershipId = membership.Id,
            MemberId = membership.MemberId,
            PackageId = membership.PackageId,
            PackageName = membership.Package.PackageName,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status,
            CreatedAt = membership.CreatedAt,
            UpdatedAt = membership.UpdatedAt
        };
    }

    // GET: /api/members/me/memberships
    // Lấy danh sách membership của member đang đăng nhập
    public async Task<Base.Response.PageResult<Response.MembershipResponse>>
        GetMyMembershipsAsync(int pageSize, int pageIndex)
    {
        if (pageSize <= 0 || pageIndex <= 0)
        {
            throw new ArgumentException(
                "PageSize and PageIndex must be greater than 0");
        }

        var memberId = await GetCurrentMemberIdAsync();

        var query = _dbContext.Memberships
            .Where(x =>
                x.MemberId == memberId &&
                !x.IsDeleted);

        var totalItems = await query.CountAsync();

        var memberships = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.MembershipResponse
            {
                MembershipId = x.Id,
                MemberId = x.MemberId,
                PackageId = x.PackageId,
                PackageName = x.Package.PackageName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.MembershipResponse>
        {
            Items = memberships,
            TotalItems = totalItems,
            PageSize = pageSize,
            PageIndex = pageIndex
        };
    }

    // GET: /api/members/{memberId}/memberships
    // Lấy danh sách membership của một member
    public async Task<Base.Response.PageResult<Response.MembershipResponse>>
        GetByMemberIdAsync(
            Guid memberId,
            int pageSize,
            int pageIndex)
    {
        if (pageSize <= 0 || pageIndex <= 0)
        {
            throw new ArgumentException(
                "PageSize and PageIndex must be greater than 0");
        }

        var memberExists = await _dbContext.Members
            .AnyAsync(x =>
                x.Id == memberId &&
                !x.IsDeleted);

        if (!memberExists)
        {
            throw new KeyNotFoundException(
                "Member not found");
        }

        var query = _dbContext.Memberships
            .Where(x =>
                x.MemberId == memberId &&
                !x.IsDeleted);

        var totalItems = await query.CountAsync();

        var memberships = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.MembershipResponse
            {
                MembershipId = x.Id,
                MemberId = x.MemberId,
                PackageId = x.PackageId,
                PackageName = x.Package.PackageName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.MembershipResponse>
        {
            Items = memberships,
            TotalItems = totalItems,
            PageSize = pageSize,
            PageIndex = pageIndex
        };
    }

    // GET: /api/memberships/{id}
    // Lấy chi tiết membership
    public async Task<Response.MembershipResponse?> GetByIdAsync(Guid id)
    {
        var membership = await _dbContext.Memberships
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new Response.MembershipResponse
            {
                MembershipId = x.Id,
                MemberId = x.MemberId,
                PackageId = x.PackageId,
                PackageName = x.Package.PackageName,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return membership;
    }

    // GET: /api/memberships/{id}/status
    // Kiểm tra trạng thái membership
    public async Task<Response.MembershipResponse?> GetStatusAsync(Guid id)
    {
        var membership = await _dbContext.Memberships
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (membership == null)
        {
            return null;
        }

        return new Response.MembershipResponse
        {
            MembershipId = membership.Id,
            Status = membership.Status,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate
        };
    }

    // POST: /api/memberships
    // Đăng ký / mua gói
    public async Task<Response.MembershipResponse> CreateAsync(
        Request.MembershipRequest request)
    {
        if (request.StartDate.Date > request.EndDate.Date)
        {
            throw new ArgumentException(
                "StartDate must be before or equal to EndDate");
        }

        var memberExists = await _dbContext.Members
            .AnyAsync(x =>
                x.Id == request.MemberId &&
                !x.IsDeleted);

        if (!memberExists)
        {
            throw new KeyNotFoundException(
                "Member not found");
        }

        var package = await _dbContext.MembershipPackages
            .FirstOrDefaultAsync(x =>
                x.Id == request.PackageId &&
                !x.IsDeleted);

        if (package == null)
        {
            throw new KeyNotFoundException(
                "Membership package not found");
        }

        var membership = new Repository.Entity.Membership
        {
            Id = Guid.NewGuid(),
            MemberId = request.MemberId,
            PackageId = request.PackageId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Memberships.Add(membership);

        await _dbContext.SaveChangesAsync();

        // Load navigation Package để lấy PackageName
        await _dbContext.Entry(membership)
            .Reference(x => x.Package)
            .LoadAsync();

        return MapToResponse(membership);
    }

    // POST: /api/memberships/{id}/renew
    // Gia hạn membership
    public async Task<Response.MembershipResponse?> RenewAsync(Guid id)
    {
        var membership = await _dbContext.Memberships
            .Include(x => x.Package)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (membership == null)
        {
            return null;
        }

        // Gia hạn từ ngày hết hạn nếu vẫn còn hạn,
        // hoặc từ ngày hiện tại nếu membership đã hết hạn.
        var today = DateTime.UtcNow.Date;

        var newStartDate = membership.EndDate.Date >= today
            ? membership.EndDate.Date.AddDays(1)
            : today;

        membership.StartDate = newStartDate;
        membership.EndDate = newStartDate.AddDays(
            membership.Package.DurationDays - 1);

        membership.UpdatedAt = DateTimeOffset.UtcNow;

        // Đặt lại trạng thái Active nếu enum có giá trị này.
        if (Enum.TryParse<MembershipStatus>(
                "Active", true, out var activeStatus))
        {
            membership.Status = activeStatus;
        }

        await _dbContext.SaveChangesAsync();

        return MapToResponse(membership);
    }

    // POST: /api/memberships/{id}/cancel
    // Hủy membership
    public async Task<bool> CancelAsync(Guid id)
    {
        var membership = await _dbContext.Memberships
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (membership == null)
        {
            return false;
        }

        // Chỉ hủy nếu enum MembershipStatus có giá trị Cancelled.
        if (!Enum.TryParse<MembershipStatus>(
                "Cancelled", true, out var cancelledStatus))
        {
            throw new InvalidOperationException(
                "MembershipStatus enum does not contain Cancelled");
        }

        membership.Status = cancelledStatus;
        membership.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }
}