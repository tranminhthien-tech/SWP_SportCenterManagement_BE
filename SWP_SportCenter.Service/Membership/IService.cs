namespace SWP_SportCenter.Service.Membership;

public interface IService
{
    // GET: /api/members/me/memberships
    // Lấy danh sách gói của member đang đăng nhập
    public Task<Base.Response.PageResult<Response.MembershipResponse>>
        GetMyMembershipsAsync(int pageSize, int pageIndex);

    // GET: /api/members/{memberId}/memberships
    // Lấy danh sách gói của một member
    public Task<Base.Response.PageResult<Response.MembershipResponse>>
        GetByMemberIdAsync(Guid memberId, int pageSize, int pageIndex);

    // GET: /api/memberships/{id}
    // Lấy chi tiết membership
    public Task<Response.MembershipResponse?> GetByIdAsync(Guid id);

    // GET: /api/memberships/{id}/status
    // Kiểm tra trạng thái membership
    public Task<Response.MembershipResponse?> GetStatusAsync(Guid id);

    // POST: /api/memberships
    // Đăng ký / mua gói
    public Task<Response.MembershipResponse>
        CreateAsync(Request.MembershipRequest request);

    // POST: /api/memberships/{id}/renew
    // Gia hạn membership
    public Task<Response.MembershipResponse?>
        RenewAsync(Guid id);

    // POST: /api/memberships/{id}/cancel
    // Hủy membership
    public Task<bool> CancelAsync(Guid id);
}