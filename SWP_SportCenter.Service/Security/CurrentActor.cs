using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Security;

// FIX B4 B5 B7 B15: định danh người dùng tại server, không tin ID từ client.
public sealed class CurrentActor(AppDbContext db, IHttpContextAccessor http)
{
    public bool IsMember => http.HttpContext?.User.IsInRole(nameof(AccountRole.Member)) == true;
    // FIX 23/10: kiểm tra quyền nghiệp vụ cả trong Service.
    public bool IsCoach => http.HttpContext?.User.IsInRole(nameof(AccountRole.Coach)) == true;
    // FIX B50: kiểm tra nguồn token ở service; middleware còn đối chiếu role hiện tại trong DB.
    public bool IsManager => http.HttpContext?.User.IsInRole(nameof(AccountRole.CenterManager)) == true;
    public bool IsReceptionist => http.HttpContext?.User.IsInRole(nameof(AccountRole.Receptionist)) == true; // FIX B58
    // FIX B39: xác nhận thủ công chỉ dành cho quản lý/lễ tân, kiểm tra cả Service.
    public bool CanConfirmPayment => http.HttpContext?.User.IsInRole("CenterManager") == true ||
        http.HttpContext?.User.IsInRole("Receptionist") == true;
    // FIX B31: phân biệt người xử lý yêu cầu hỗ trợ với Coach chỉ có quyền chuyên môn.
    public bool CanHandleSupport => http.HttpContext?.User.IsInRole(nameof(AccountRole.Receptionist)) == true ||
        http.HttpContext?.User.IsInRole(nameof(AccountRole.CenterManager)) == true;
    public Guid AccountId => Guid.TryParse(http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
        ? id : throw new UnauthorizedAccessException("Không tìm thấy AccountId trong token.");

    public async Task<Guid> MemberIdAsync()
        => await db.Members.Where(m => m.AccountId == AccountId && !m.IsDeleted && !m.Account.IsDeleted)
            .Select(m => (Guid?)m.Id).FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Không tìm thấy hồ sơ Member.");

    public async Task EnsureCanViewMemberAsync(Guid memberId)
    {
        if (IsMember && memberId != await MemberIdAsync())
            throw new UnauthorizedAccessException("Bạn chỉ được xem dữ liệu của chính mình.");
    }
}
