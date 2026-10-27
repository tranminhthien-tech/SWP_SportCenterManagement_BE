using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;

namespace SWP_SportCenter.Service.TrainingPlan;

public class Service: IService
{
    // FIX B49: không loop gọi CreateAsync (mỗi học viên một transaction) như workaround FE.
    public async Task<List<Response.TrainingPlanResponse>> CreateForClassAsync(Guid classId, Request.ClassTrainingPlanRequest request)
    {
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        var coachId = await GetCurrentCoachIdAsync();
        var cls = await _dbContext.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId && !c.IsDeleted);
        if (cls == null) throw new KeyNotFoundException("Không tìm thấy lớp học.");
        if (cls.CoachId != coachId) throw new UnauthorizedAccessException("Bạn chỉ được tạo kế hoạch cho lớp mình dạy.");
        var ids = await _dbContext.ClassBookings.Where(b => b.ClassId == classId && !b.IsDeleted &&
                b.Status != Repository.Enum.BookingStatus.Cancelled && !b.Member.IsDeleted && !b.Member.Account.IsDeleted &&
                b.Member.Account.Role == Repository.Enum.AccountRole.Member && b.Member.Account.Status == Repository.Enum.AccountStatus.Active)
            .Select(b => b.MemberId).Distinct().ToListAsync();
        if (ids.Count == 0) throw new ArgumentException("Lớp chưa có học viên đang đăng ký.");
        var plans = ids.Select(memberId => new Repository.Entity.TrainingPlan {
            Id = Guid.NewGuid(), ClassId = classId, CoachId = coachId, MemberId = memberId,
            Title = request.Title.Trim(), ContentDescription = request.ContentDescription,
            CreatedDate = request.CreatedDate
        }).ToList();
        _dbContext.TrainingPlans.AddRange(plans);
        await _dbContext.SaveChangesAsync(); // một transaction cho cả lớp và audit khi dùng PostgreSQL.
        var planIds = plans.Select(p => p.Id).ToList();
        return await _dbContext.TrainingPlans.AsNoTracking().Where(p => planIds.Contains(p.Id))
            .Select(p => new Response.TrainingPlanResponse {
                TrainingPlanId = p.Id, CoachId = p.CoachId, MemberId = p.MemberId, ClassId = p.ClassId,
                CoachName = p.Coach.FullName, MemberName = p.Member.FullName, ClassName = p.Class.ClassName,
                Title = p.Title, ContentDescription = p.ContentDescription, CreatedDate = p.CreatedDate,
                CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt
            }).ToListAsync();
    }
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Service(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
    }

    // Lấy AccountId từ token
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

    // Lấy CoachId của coach đang đăng nhập
    private async Task<Guid> GetCurrentCoachIdAsync()
    {
        var accountId = GetCurrentAccountId();

        var coach = await _dbContext.Coaches
            .FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                !x.IsDeleted);

        if (coach == null)
        {
            throw new KeyNotFoundException("Coach not found");
        }

        return coach.Id;
    }

    // Lấy MemberId của member đang đăng nhập
    private async Task<Guid> GetCurrentMemberIdAsync()
    {
        var accountId = GetCurrentAccountId();

        var member = await _dbContext.Members
            .FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                !x.IsDeleted);

        if (member == null)
        {
            throw new KeyNotFoundException("Member not found");
        }

        return member.Id;
    }

    // Chuyển Entity sang Response
    private static Response.TrainingPlanResponse MapToResponse(
        Repository.Entity.TrainingPlan trainingPlan)
    {
        return new Response.TrainingPlanResponse
        {
            TrainingPlanId = trainingPlan.Id,
            CoachId = trainingPlan.CoachId,
            MemberId = trainingPlan.MemberId,
            ClassId = trainingPlan.ClassId,
            // FIX B13: kèm tên hiển thị để FE không ghép ID.
            MemberName = trainingPlan.Member.FullName,
            CoachName = trainingPlan.Coach.FullName,
            ClassName = trainingPlan.Class.ClassName,
            Title = trainingPlan.Title,
            ContentDescription = trainingPlan.ContentDescription,
            CreatedDate = trainingPlan.CreatedDate,
            CreatedAt = trainingPlan.CreatedAt,
            UpdatedAt = trainingPlan.UpdatedAt
        };
    }

    // GET: /api/training-plans/{id}
    // Lấy chi tiết kế hoạch
    public async Task<Response.TrainingPlanResponse?> GetByIdAsync(
        Guid id)
    {
        var trainingPlan = await _dbContext.TrainingPlans
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new Response.TrainingPlanResponse
            {
                TrainingPlanId = x.Id,
                CoachId = x.CoachId,
                MemberId = x.MemberId,
                ClassId = x.ClassId,
                // FIX B13: kèm tên hiển thị để FE không ghép ID.
                MemberName = x.Member.FullName,
                CoachName = x.Coach.FullName,
                ClassName = x.Class.ClassName,
                Title = x.Title,
                ContentDescription = x.ContentDescription,
                CreatedDate = x.CreatedDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return trainingPlan;
    }

    // GET: /api/coaches/me/training-plans
    // Lấy danh sách kế hoạch của coach đang đăng nhập
    public async Task<Base.Response.PageResult<Response.TrainingPlanResponse>>
        GetMyTrainingPlansAsCoachAsync(
            string? searchTerm,
            int pageSize,
            int pageIndex)
    {
        if (pageSize <= 0 || pageIndex <= 0)
        {
            throw new ArgumentException(
                "PageSize and PageIndex must be greater than 0");
        }

        var coachId = await GetCurrentCoachIdAsync();

        var query = _dbContext.TrainingPlans
            .Where(x =>
                x.CoachId == coachId &&
                !x.IsDeleted);

        // Tìm kiếm theo tiêu đề hoặc nội dung
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.Title.Contains(searchTerm) ||
                x.ContentDescription.Contains(searchTerm));
        }

        // Đếm tổng số bản ghi trước khi phân trang
        var totalItems = await query.CountAsync();

        var trainingPlans = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.TrainingPlanResponse
            {
                TrainingPlanId = x.Id,
                CoachId = x.CoachId,
                MemberId = x.MemberId,
                ClassId = x.ClassId,
                // FIX B13: kèm tên hiển thị để FE không ghép ID.
                MemberName = x.Member.FullName,
                CoachName = x.Coach.FullName,
                ClassName = x.Class.ClassName,
                Title = x.Title,
                ContentDescription = x.ContentDescription,
                CreatedDate = x.CreatedDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.TrainingPlanResponse>
        {
            Data = trainingPlans,
            TotalItems = totalItems,
            PageSize = pageSize,
            PageIndex = pageIndex
        };
    }

    // GET: /api/members/me/training-plans
    // Lấy danh sách kế hoạch của member đang đăng nhập
    public async Task<Base.Response.PageResult<Response.TrainingPlanResponse>>
        GetMyTrainingPlansAsMemberAsync(
            string? searchTerm,
            int pageSize,
            int pageIndex)
    {
        if (pageSize <= 0 || pageIndex <= 0)
        {
            throw new ArgumentException(
                "PageSize and PageIndex must be greater than 0");
        }

        var memberId = await GetCurrentMemberIdAsync();

        var query = _dbContext.TrainingPlans
            .Where(x =>
                x.MemberId == memberId &&
                !x.IsDeleted);

        // Tìm kiếm theo tiêu đề hoặc nội dung
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.Title.Contains(searchTerm) ||
                x.ContentDescription.Contains(searchTerm));
        }

        var totalItems = await query.CountAsync();

        var trainingPlans = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.TrainingPlanResponse
            {
                TrainingPlanId = x.Id,
                CoachId = x.CoachId,
                MemberId = x.MemberId,
                ClassId = x.ClassId,
                // FIX B13: kèm tên hiển thị để FE không ghép ID.
                MemberName = x.Member.FullName,
                CoachName = x.Coach.FullName,
                ClassName = x.Class.ClassName,
                Title = x.Title,
                ContentDescription = x.ContentDescription,
                CreatedDate = x.CreatedDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.TrainingPlanResponse>
        {
            Data = trainingPlans,
            TotalItems = totalItems,
            PageSize = pageSize,
            PageIndex = pageIndex
        };
    }

    // POST: /api/training-plans
    // Tạo kế hoạch
    public async Task<Response.TrainingPlanResponse> CreateAsync(
        Request.TrainingPlanRequest request)
    {
        // Đảm bảo coach chỉ tạo kế hoạch cho chính mình
        var coachId = await GetCurrentCoachIdAsync();

        if (request.CoachId != coachId)
        {
            throw new UnauthorizedAccessException(
                "You can only create your own training plan");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException(
                "Training plan title is required");
        }

        var memberExists = await _dbContext.Members
            .AnyAsync(x =>
                x.Id == request.MemberId &&
                !x.IsDeleted);

        if (!memberExists)
        {
            throw new KeyNotFoundException("Member not found");
        }

        var classExists = await _dbContext.Classes
            .AnyAsync(x =>
                x.Id == request.ClassId &&
                !x.IsDeleted);

        if (!classExists)
        {
            throw new KeyNotFoundException("Class not found");
        }

        var trainingPlan = new Repository.Entity.TrainingPlan
        {
            Id = Guid.NewGuid(),
            CoachId = coachId,
            MemberId = request.MemberId,
            ClassId = request.ClassId,
            Title = request.Title,
            ContentDescription = request.ContentDescription,
            CreatedDate = request.CreatedDate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.TrainingPlans.Add(trainingPlan);

        await _dbContext.SaveChangesAsync();

        // CODE CŨ: return MapToResponse(trainingPlan);
        // FIX B13: query projection để lấy navigation names sau khi tạo.
        return (await GetByIdAsync(trainingPlan.Id))!;
    }

    // PUT: /api/training-plans/{id}
    // Cập nhật kế hoạch
    public async Task<bool> UpdateAsync(
        Guid id,
        Request.TrainingPlanRequest request)
    {
        var coachId = await GetCurrentCoachIdAsync();

        var trainingPlan = await _dbContext.TrainingPlans
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (trainingPlan == null)
        {
            return false;
        }

        // Chỉ cho phép coach sở hữu kế hoạch cập nhật
        if (trainingPlan.CoachId != coachId)
        {
            throw new UnauthorizedAccessException(
                "You can only update your own training plan");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException(
                "Training plan title is required");
        }

        var memberExists = await _dbContext.Members
            .AnyAsync(x =>
                x.Id == request.MemberId &&
                !x.IsDeleted);

        if (!memberExists)
        {
            throw new KeyNotFoundException("Member not found");
        }

        var classExists = await _dbContext.Classes
            .AnyAsync(x =>
                x.Id == request.ClassId &&
                !x.IsDeleted);

        if (!classExists)
        {
            throw new KeyNotFoundException("Class not found");
        }

        trainingPlan.MemberId = request.MemberId;
        trainingPlan.ClassId = request.ClassId;
        trainingPlan.Title = request.Title;
        trainingPlan.ContentDescription = request.ContentDescription;
        trainingPlan.CreatedDate = request.CreatedDate;
        trainingPlan.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    // DELETE: /api/training-plans/{id}
    // Xóa kế hoạch (soft delete)
    public async Task<string> DeleteAsync(Guid id)
    {
        var coachId = await GetCurrentCoachIdAsync();

        var trainingPlan = await _dbContext.TrainingPlans
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (trainingPlan == null)
        {
            return "Training plan not found";
        }

        // Chỉ cho phép coach sở hữu kế hoạch xóa
        if (trainingPlan.CoachId != coachId)
        {
            throw new UnauthorizedAccessException(
                "You can only delete your own training plan");
        }

        trainingPlan.IsDeleted = true;
        trainingPlan.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Training plan deleted successfully";
    }
}
