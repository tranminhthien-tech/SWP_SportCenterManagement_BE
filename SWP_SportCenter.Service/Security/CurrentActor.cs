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
