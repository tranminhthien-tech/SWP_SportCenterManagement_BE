using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Accounts;

public interface IAccountService
{
    Task<Base.Response.PageResult<Response.AccountResponse>> GetAllAsync(
        string? searchTerm,
        AccountRole? role,
        AccountStatus? status,
        int pageSize,
        int pageIndex);

    Task<Response.AccountResponse?> GetByIdAsync(Guid id);
    Task<bool> UpdateAsync(Guid id, Request.UpdateAccountRequest request);
    Task<bool> SoftDeleteAsync(Guid id);
}
