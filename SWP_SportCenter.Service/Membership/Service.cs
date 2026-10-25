using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Membership;

public class Service: IService
{
    // FIX B20: sửa kỳ hạn; không thay đổi chủ sở hữu hoặc gói của hóa đơn.
    public async Task<bool> UpdateAsync(Guid id, Request.UpdateMembershipRequest request)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        var membership = await _dbContext.Memberships.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (membership == null) return false;
        membership.StartDate = request.StartDate; membership.EndDate = request.EndDate; membership.Status = request.Status;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    // FIX B20: xóa mềm giữ liên kết hóa đơn và dữ liệu lịch sử.
    public async Task<bool> DeleteAsync(Guid id)
    {
        var membership = await _dbContext.Memberships.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (membership == null) return false;
        membership.IsDeleted = true; membership.Status = MembershipStatus.Cancelled;
        await _dbContext.SaveChangesAsync();
        return true;
    }
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
            MemberName = membership.Member?.FullName ?? string.Empty,
            PackageId = membership.PackageId,
            PackageName = membership.Package.PackageName,
            StartDate = membership.StartDate,
            EndDate = membership.EndDate,
            Status = membership.Status,
            CreatedAt = membership.CreatedAt,
            UpdatedAt = membership.UpdatedAt
        };
    }

   // GET: /api/memberships
// Lễ tân/quản lý tra cứu tất cả lượt đăng ký gói tập
public async Task<Base.Response.PageResult<Response.MembershipResponse>>
    GetAllAsync(
        string? searchTerm,
        MembershipStatus? status,
        Guid? memberId,
        Guid? packageId,
        int pageSize,
        int pageIndex)
{
    if (pageSize <= 0 || pageIndex <= 0)
    {
        throw new ArgumentException(
            "PageSize and PageIndex must be greater than 0");
    }

    var query = _dbContext.Memberships
        .AsNoTracking()
        .Where(x => !x.IsDeleted);

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        var keyword = searchTerm.Trim();

        query = query.Where(x =>
            x.Member.FullName.Contains(keyword) ||
            // CODE CŨ: x.Member.Phone.Contains(keyword)
            // FIX B18: phone cũ có thể chưa biết.
            (x.Member.Phone != null && x.Member.Phone.Contains(keyword)) ||
            x.Member.Account.Email.Contains(keyword) ||
            x.Package.PackageName.Contains(keyword));
    }

    if (status.HasValue)
    {
        query = query.Where(x => x.Status == status.Value);
    }

    if (memberId.HasValue)
    {
        query = query.Where(x => x.MemberId == memberId.Value);
    }

    if (packageId.HasValue)
    {
        query = query.Where(x => x.PackageId == packageId.Value);
    }

    var totalItems = await query.CountAsync();

    var memberships = await query
        .OrderByDescending(x => x.CreatedAt)
        .Skip((pageIndex - 1) * pageSize)
        .Take(pageSize)
        .Select(x => new Response.MembershipResponse
        {
            MembershipId = x.Id,
            MemberId = x.MemberId,
            MemberName = x.Member.FullName,
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
        Data = memberships,
        TotalItems = totalItems,
        PageSize = pageSize,
        PageIndex = pageIndex
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
                MemberName = x.Member.FullName,
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
            Data = memberships,
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
                MemberName = x.Member.FullName,
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
            Data = memberships,
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
                MemberName = x.Member.FullName,
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
    public async Task<Response.MembershipResponse?> RenewAsync(Guid id, Guid? receptionistId = null)
    {
        // FIX B20: chống hai yêu cầu gia hạn tạo kỳ trùng nhau trên PostgreSQL.
        await using var transaction = _dbContext.Database.IsRelational()
            ? await _dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable) : null;
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

        // CODE CŨ: membership.StartDate = newStartDate;
        // CODE CŨ: membership.EndDate = newStartDate.AddDays(
        // CODE CŨ: membership.Package.DurationDays - 1);
        // CODE CŨ: 
        // CODE CŨ: membership.UpdatedAt = DateTimeOffset.UtcNow;
        // CODE CŨ: 
        // CODE CŨ: // Đặt lại trạng thái Active nếu enum có giá trị này.
        // CODE CŨ: if (Enum.TryParse<MembershipStatus>(
        // CODE CŨ: "Active", true, out var activeStatus))
        // CODE CŨ: {
        // CODE CŨ: membership.Status = activeStatus;
        // CODE CŨ: }
        // CODE CŨ: 
        // CODE CŨ: await _dbContext.SaveChangesAsync();
        // CODE CŨ: 
        // CODE CŨ: return MapToResponse(membership);
        // FIX B20: tạo kỳ mới và hóa đơn Pending trong cùng SaveChanges transaction.
        if (membership.Package.DurationDays <= 0) throw new ArgumentException("Gói tập có thời hạn không hợp lệ.");
        var accountId = GetCurrentAccountId();
        var ownReceptionist = await _dbContext.Receptionists.Where(r => r.AccountId == accountId && !r.IsDeleted)
            .Select(r => (Guid?)r.Id).FirstOrDefaultAsync();
        var staffId = ownReceptionist ?? receptionistId;
        if (!staffId.HasValue || !await _dbContext.Receptionists.AnyAsync(r => r.Id == staffId.Value &&
                !r.IsDeleted && !r.Account.IsDeleted && r.Account.Status == AccountStatus.Active))
            throw new ArgumentException("Manager cần truyền receptionistId của Lễ tân đang hoạt động để lập hóa đơn.");
        var existingEnd = await _dbContext.Memberships.Where(m => m.MemberId == membership.MemberId &&
            m.PackageId == membership.PackageId && !m.IsDeleted && m.Status == MembershipStatus.Active)
            .MaxAsync(m => (DateTime?)m.EndDate);
        if (existingEnd.HasValue && existingEnd.Value.Date > membership.EndDate.Date)
            newStartDate = existingEnd.Value.Date.AddDays(1);
        var renewed = new Repository.Entity.Membership {
            Id = Guid.NewGuid(), MemberId = membership.MemberId, PackageId = membership.PackageId,
            Package = membership.Package, StartDate = newStartDate,
            EndDate = newStartDate.AddDays(membership.Package.DurationDays - 1), Status = MembershipStatus.Active
        };
        _dbContext.Memberships.Add(renewed);
        _dbContext.Invoices.Add(new Repository.Entity.Invoice {
            Id = Guid.NewGuid(), ReceptionistId = staffId.Value, Membership = renewed,
            Amount = membership.Package.Price, Status = InvoiceStatus.Pending,
            // CODE CŨ: PaymentDate = DateTimeOffset.UtcNow, PaymentMethod = (PaymentMethod)1
            // FIX B39: hóa đơn gia hạn Pending chưa có ngày thanh toán.
            PaymentDate = null, PaymentMethod = (PaymentMethod)1
        });
        await _dbContext.SaveChangesAsync();
        await _dbContext.Entry(renewed).Reference(x => x.Member).LoadAsync();
        if (transaction != null) await transaction.CommitAsync();
        return MapToResponse(renewed);
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
            // CODE CŨ: throw new InvalidOperationException(
            //                 "MembershipStatus enum does not contain Cancelled");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException(
                "MembershipStatus enum does not contain Cancelled");
        }

        membership.Status = cancelledStatus;
        membership.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }
}
