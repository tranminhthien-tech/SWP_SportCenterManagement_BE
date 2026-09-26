using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;

namespace SWP_SportCenter.Service.TrainingResult;

public class Service:IService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Service(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor)
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

    // Lấy CoachId từ AccountId
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

    // Lấy MemberId từ AccountId
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
    private static Response.TrainingResultResponse MapToResponse(
        Repository.Entity.TrainingResult trainingResult)
    {
        return new Response.TrainingResultResponse
        {
            TrainingResultId = trainingResult.Id,
            PlanId = trainingResult.PlanId,
            MemberId = trainingResult.MemberId,
            CoachId = trainingResult.CoachId,
            PerformanceScore = trainingResult.PerformanceScore,
            CoachComment = trainingResult.CoachComment,
            EvaluationDate = trainingResult.EvaluationDate,
            CreatedAt = trainingResult.CreatedAt,
            UpdatedAt = trainingResult.UpdatedAt
        };
    }

    // GET: /api/training-results/{id}
    // Lấy chi tiết kết quả
    public async Task<Response.TrainingResultResponse?> GetByIdAsync(
        Guid id)
    {
        var trainingResult = await _dbContext.TrainingResults
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new Response.TrainingResultResponse
            {
                TrainingResultId = x.Id,
                PlanId = x.PlanId,
                MemberId = x.MemberId,
                CoachId = x.CoachId,
                PerformanceScore = x.PerformanceScore,
                CoachComment = x.CoachComment,
                EvaluationDate = x.EvaluationDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return trainingResult;
    }

    // GET: /api/members/me/training-results
    // Lấy lịch sử kết quả của Member đang đăng nhập
    public async Task<Base.Response.PageResult<Response.TrainingResultResponse>>
        GetMyTrainingResultsAsync(
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

        var query = _dbContext.TrainingResults
            .Where(x =>
                x.MemberId == memberId &&
                !x.IsDeleted);

        // Tìm kiếm theo nhận xét của Coach
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.CoachComment.Contains(searchTerm));
        }

        // Đếm tổng số bản ghi trước khi phân trang
        var totalItems = await query.CountAsync();

        var trainingResults = await query
            .OrderByDescending(x => x.EvaluationDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.TrainingResultResponse
            {
                TrainingResultId = x.Id,
                PlanId = x.PlanId,
                MemberId = x.MemberId,
                CoachId = x.CoachId,
                PerformanceScore = x.PerformanceScore,
                CoachComment = x.CoachComment,
                EvaluationDate = x.EvaluationDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.TrainingResultResponse>
        {
            Items = trainingResults,
            TotalItems = totalItems,
            PageSize = pageSize,
            PageIndex = pageIndex
        };
    }

    // GET: /api/coaches/me/members/{memberId}/training-results
    // Lấy tiến độ tập luyện của một Member
    public async Task<Base.Response.PageResult<Response.TrainingResultResponse>>
        GetByMemberIdAsync(
            Guid memberId,
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

        // Kiểm tra Member có tồn tại không
        var memberExists = await _dbContext.Members
            .AnyAsync(x =>
                x.Id == memberId &&
                !x.IsDeleted);

        if (!memberExists)
        {
            throw new KeyNotFoundException("Member not found");
        }

        // Chỉ lấy kết quả của Member thuộc kế hoạch
        // do Coach đang đăng nhập phụ trách
        var query = _dbContext.TrainingResults
            .Where(x =>
                x.MemberId == memberId &&
                x.CoachId == coachId &&
                !x.IsDeleted);

        // Tìm kiếm theo nhận xét của Coach
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x =>
                x.CoachComment.Contains(searchTerm));
        }

        var totalItems = await query.CountAsync();

        var trainingResults = await query
            .OrderByDescending(x => x.EvaluationDate)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new Response.TrainingResultResponse
            {
                TrainingResultId = x.Id,
                PlanId = x.PlanId,
                MemberId = x.MemberId,
                CoachId = x.CoachId,
                PerformanceScore = x.PerformanceScore,
                CoachComment = x.CoachComment,
                EvaluationDate = x.EvaluationDate,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.TrainingResultResponse>
        {
            Items = trainingResults,
            TotalItems = totalItems,
            PageSize = pageSize,
            PageIndex = pageIndex
        };
    }

    // POST: /api/training-results
    // Ghi nhận kết quả / nhận xét
    public async Task<Response.TrainingResultResponse> CreateAsync(
        Request.TrainingResultRequest request)
    {
        var coachId = await GetCurrentCoachIdAsync();

        if (string.IsNullOrWhiteSpace(request.CoachComment))
        {
            throw new ArgumentException(
                "Coach comment is required");
        }

        // Kiểm tra kế hoạch có tồn tại và thuộc Coach hiện tại
        var trainingPlan = await _dbContext.TrainingPlans
            .FirstOrDefaultAsync(x =>
                x.Id == request.PlanId &&
                !x.IsDeleted);

        if (trainingPlan == null)
        {
            throw new KeyNotFoundException(
                "Training plan not found");
        }

        if (trainingPlan.CoachId != coachId)
        {
            throw new UnauthorizedAccessException(
                "You can only evaluate your own training plan");
        }

        // Member phải đúng với Member trong kế hoạch
        if (trainingPlan.MemberId != request.MemberId)
        {
            throw new ArgumentException(
                "Member does not belong to this training plan");
        }

        var trainingResult = new Repository.Entity.TrainingResult
        {
            Id = Guid.NewGuid(),
            PlanId = trainingPlan.Id,
            MemberId = trainingPlan.MemberId,
            CoachId = coachId,
            PerformanceScore = request.PerformanceScore,
            CoachComment = request.CoachComment,
            EvaluationDate = request.EvaluationDate,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.TrainingResults.Add(trainingResult);

        await _dbContext.SaveChangesAsync();

        return MapToResponse(trainingResult);
    }

    // PUT: /api/training-results/{id}
    // Cập nhật kết quả
    public async Task<bool> UpdateAsync(
        Guid id,
        Request.TrainingResultRequest request)
    {
        var coachId = await GetCurrentCoachIdAsync();

        var trainingResult = await _dbContext.TrainingResults
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (trainingResult == null)
        {
            return false;
        }

        // Chỉ Coach tạo kết quả mới được cập nhật
        if (trainingResult.CoachId != coachId)
        {
            throw new UnauthorizedAccessException(
                "You can only update your own training result");
        }

        // Kiểm tra kế hoạch mới có tồn tại và thuộc Coach
        var trainingPlan = await _dbContext.TrainingPlans
            .FirstOrDefaultAsync(x =>
                x.Id == request.PlanId &&
                !x.IsDeleted);

        if (trainingPlan == null)
        {
            throw new KeyNotFoundException(
                "Training plan not found");
        }

        if (trainingPlan.CoachId != coachId)
        {
            throw new UnauthorizedAccessException(
                "You can only use your own training plan");
        }

        if (trainingPlan.MemberId != request.MemberId)
        {
            throw new ArgumentException(
                "Member does not belong to this training plan");
        }

        if (string.IsNullOrWhiteSpace(request.CoachComment))
        {
            throw new ArgumentException(
                "Coach comment is required");
        }

        trainingResult.PlanId = trainingPlan.Id;
        trainingResult.MemberId = trainingPlan.MemberId;
        trainingResult.CoachId = coachId;
        trainingResult.PerformanceScore = request.PerformanceScore;
        trainingResult.CoachComment = request.CoachComment;
        trainingResult.EvaluationDate = request.EvaluationDate;
        trainingResult.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return true;
    }

    // DELETE: /api/training-results/{id}
    // Xóa kết quả (soft delete)
    public async Task<string> DeleteAsync(Guid id)
    {
        var coachId = await GetCurrentCoachIdAsync();

        var trainingResult = await _dbContext.TrainingResults
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                !x.IsDeleted);

        if (trainingResult == null)
        {
            return "Training result not found";
        }

        // Chỉ Coach sở hữu kết quả mới được xóa
        if (trainingResult.CoachId != coachId)
        {
            throw new UnauthorizedAccessException(
                "You can only delete your own training result");
        }

        trainingResult.IsDeleted = true;
        trainingResult.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        return "Training result deleted successfully";
    }
}