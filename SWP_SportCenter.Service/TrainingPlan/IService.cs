namespace SWP_SportCenter.Service.TrainingPlan;

public interface IService
{
    // GET: /api/training-plans/{id}
    // Lấy chi tiết kế hoạch
    public Task<Response.TrainingPlanResponse?> GetByIdAsync(Guid id);

    // GET: /api/coaches/me/training-plans
    // Lấy danh sách kế hoạch của coach đang đăng nhập
    public Task<Base.Response.PageResult<Response.TrainingPlanResponse>>
        GetMyTrainingPlansAsCoachAsync(
            string? searchTerm,
            int pageSize,
            int pageIndex);

    // GET: /api/members/me/training-plans
    // Lấy danh sách kế hoạch của member đang đăng nhập
    public Task<Base.Response.PageResult<Response.TrainingPlanResponse>>
        GetMyTrainingPlansAsMemberAsync(
            string? searchTerm,
            int pageSize,
            int pageIndex);

    // POST: /api/training-plans
    // Tạo kế hoạch
    public Task<Response.TrainingPlanResponse>
        CreateAsync(Request.TrainingPlanRequest request);

    // PUT: /api/training-plans/{id}
    // Cập nhật kế hoạch
    public Task<bool> UpdateAsync(
        Guid id,
        Request.TrainingPlanRequest request);

    // DELETE: /api/training-plans/{id}
    // Xóa kế hoạch
    public Task<string> DeleteAsync(Guid id);
}