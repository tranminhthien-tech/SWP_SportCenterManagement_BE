namespace SWP_SportCenter.Service.TrainingResult;

public interface IService
{
    // GET: /api/training-results/{id}
    // Lấy chi tiết kết quả tập luyện
    public Task<Response.TrainingResultResponse?>
        GetByIdAsync(Guid id);

    // GET: /api/members/me/training-results
    // Lấy lịch sử kết quả của member đang đăng nhập
    public Task<Base.Response.PageResult<Response.TrainingResultResponse>>
        GetMyTrainingResultsAsync(
            string? searchTerm,
            int pageSize,
            int pageIndex);

    // GET: /api/coaches/me/members/{memberId}/training-results
    // Lấy tiến độ tập luyện của một member
    public Task<Base.Response.PageResult<Response.TrainingResultResponse>>
        GetByMemberIdAsync(
            Guid memberId,
            string? searchTerm,
            int pageSize,
            int pageIndex);

    // POST: /api/training-results
    // Ghi nhận kết quả / nhận xét
    public Task<Response.TrainingResultResponse>
        CreateAsync(Request.TrainingResultRequest request);

    // PUT: /api/training-results/{id}
    // Cập nhật kết quả
    public Task<bool> UpdateAsync(
        Guid id,
        Request.TrainingResultRequest request);

    // DELETE: /api/training-results/{id}
    // Xóa kết quả nếu nghiệp vụ cho phép
    public Task<string> DeleteAsync(Guid id);
}