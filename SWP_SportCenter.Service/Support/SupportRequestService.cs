using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Service.Security;

namespace SWP_SportCenter.Service.Support;

// FIX B31: Member chỉ tạo/xem yêu cầu của mình; xử lý dành cho lễ tân/manager ở Controller.
public sealed class CreateSupportRequest
{
    [Required, MaxLength(2000)] public string Content { get; set; } = string.Empty;
}
public sealed class UpdateSupportRequest
{
    [EnumDataType(typeof(SupportRequestStatus))] public SupportRequestStatus Status { get; set; }
    [MaxLength(2000)] public string? Resolution { get; set; }
}
public sealed class SupportResponse
{
    public Guid Id { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public SupportRequestStatus Status { get; set; }
    public Guid? HandledByAccountId { get; set; }
    public string? Resolution { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
public sealed class SupportRequestService(AppDbContext db, CurrentActor actor)
{
    public async Task<SupportResponse> CreateAsync(CreateSupportRequest request)
    {
        Validator.ValidateObject(request, new ValidationContext(request), true);
        if (!actor.IsMember) throw new UnauthorizedAccessException("Chỉ Member được tạo yêu cầu của mình.");
        var memberId = await actor.MemberIdAsync();
        var row = new SupportRequest { Id = Guid.NewGuid(), MemberId = memberId,
            Content = request.Content.Trim(), Status = SupportRequestStatus.Open };
        db.SupportRequests.Add(row);
        await db.SaveChangesAsync();
        return (await Query().FirstAsync(x => x.Id == row.Id));
    }

    public async Task<Base.Response.PageResult<SupportResponse>> GetAsync(int pageIndex, int pageSize,
        Guid? memberId, SupportRequestStatus? status)
    {
        if (pageIndex < 1 || pageSize < 1 || pageSize > 500 || (long)(pageIndex - 1) * pageSize > int.MaxValue)
            throw new ArgumentException("Phân trang không hợp lệ.");
        if (!actor.IsMember && !actor.CanHandleSupport)
            throw new UnauthorizedAccessException("Bạn không có quyền xem yêu cầu hỗ trợ.");
        if (status.HasValue && !Enum.IsDefined(status.Value)) throw new ArgumentException("Trạng thái không hợp lệ.");
        if (actor.IsMember) memberId = await actor.MemberIdAsync(); // bỏ qua memberId giả mạo
        var query = Query();
        if (memberId.HasValue) query = query.Where(x => x.MemberId == memberId.Value);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new() { Data = rows, TotalItems = total, PageIndex = pageIndex, PageSize = pageSize };
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateSupportRequest request)
    {
        if (!actor.CanHandleSupport) throw new UnauthorizedAccessException("Chỉ lễ tân hoặc quản lý được xử lý yêu cầu hỗ trợ.");
        Validator.ValidateObject(request, new ValidationContext(request), true);
        var row = await db.SupportRequests.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (row == null) return false;
        row.Status = request.Status;
        row.Resolution = request.Resolution?.Trim();
        row.HandledByAccountId = actor.AccountId; // Không nhận người xử lý từ body.
        await db.SaveChangesAsync();
        return true;
    }

    private IQueryable<SupportResponse> Query() => db.SupportRequests.AsNoTracking().Where(x => !x.IsDeleted)
        .Select(x => new SupportResponse { Id = x.Id, MemberId = x.MemberId, MemberName = x.Member.FullName,
            Content = x.Content, Status = x.Status, HandledByAccountId = x.HandledByAccountId,
            Resolution = x.Resolution, CreatedAt = x.CreatedAt, UpdatedAt = x.UpdatedAt });
}
