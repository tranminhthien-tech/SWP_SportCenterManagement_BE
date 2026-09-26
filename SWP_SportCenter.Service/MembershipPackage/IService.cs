namespace SWP_SportCenter.Service.MembershipPackage;

public interface IService
{
    // GET: api/membership-packages
    // Xem danh sách gói tập (có phân trang và tìm kiếm)
    public Task<Base.Response.PageResult<Response.MembershipPackageResponse>>
        GetAllAsync( string? searchTerm, int pageIndex, int pageSize);

    // GET: api/membership-packages/{id}
    // Xem chi tiết gói tập
    public Task<Response.MembershipPackageResponse?> GetByIdAsync(Guid id);

    // POST: api/membership-packages
    // Tạo gói tập
    public Task<Response.MembershipPackageResponse> CreateAsync(
        Request.MembershipPackageRequest request);

    // PUT: api/membership-packages/{id}
    // Cập nhật gói tập
    public Task<bool> UpdateAsync(
        Guid id,
        Request.MembershipPackageRequest request);

    // DELETE: api/membership-packages/{id}
    // Khóa/xóa gói tập
    public Task<bool> DeleteAsync(Guid id);
}