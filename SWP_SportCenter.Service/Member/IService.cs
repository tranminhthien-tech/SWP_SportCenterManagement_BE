namespace SWP_SportCenter.Service.Member;

public interface IService
{
    // GET /api/members
    // Manager, Receptionist
    // Danh sách + tìm kiếm member
    Task<Base.Response.PageResult<Response.MemberResponse>> GetAllAsync(
        int pageIndex,
        int pageSize,
        string? searchTerm);

    // GET /api/members/{id}
    // Manager, Receptionist, Coach
    // Chi tiết member
    Task<Response.MemberResponse?> GetByIdAsync(Guid id);

    // POST /api/members
    // Manager, Receptionist
    // Tạo member
    Task<Response.MemberResponse> CreateAsync(
        Request.MemberRequest request);

    // PUT /api/members/{id}
    // Manager
    // Cập nhật member
    Task<bool> UpdateAsync(
        Guid id,
        Request.MemberRequest request);

    // DELETE /api/members/{id}
    // Manager
    // Khóa/xóa member
    Task<bool> DeleteAsync(Guid id);

    // GET /api/members/me
    // Member
    // Xem profile
    Task<Response.MemberResponse?> GetMyProfileAsync();

    // PUT /api/members/me
    // Member
    // Cập nhật profile
    Task<bool> UpdateMyProfileAsync(
        Request.MemberRequest request);

    // GET /api/members/{id}/summary
    // Manager, Receptionist, Coach
    // Tổng quan member
    Task<Response.MemberResponse?> GetSummaryAsync(Guid id);
}