using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Accounts;

public class AccountService : IAccountService
{
    private readonly AppDbContext _dbContext;

    public AccountService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Base.Response.PageResult<Response.AccountResponse>> GetAllAsync(
        string? searchTerm,
        AccountRole? role,
        AccountStatus? status,
        int pageSize,
        int pageIndex)
    {
        if (pageSize <= 0 || pageIndex <= 0)
        {
            throw new ArgumentException(
                "PageSize and PageIndex must be greater than 0");
        }

        var query = _dbContext.Accounts
            .AsNoTracking()
            .Where(account => !account.IsDeleted);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var keyword = searchTerm.Trim();
            query = query.Where(account =>
                account.Username.Contains(keyword) ||
                account.Email.Contains(keyword));
        }

        if (role.HasValue)
        {
            query = query.Where(account => account.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(account => account.Status == status.Value);
        }

        var totalItems = await query.CountAsync();
        var accounts = await query
            .OrderByDescending(account => account.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(account => new Response.AccountResponse
            {
                AccountId = account.Id,
                Username = account.Username,

                FullName =
                    account.Member != null ? account.Member.FullName :
                    account.Coach != null ? account.Coach.FullName :
                    account.Receptionist != null ? account.Receptionist.FullName :
                    account.CenterManager != null ? account.CenterManager.FullName :
                    string.Empty,

                Email = account.Email,
                Role = account.Role,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            })
            .ToListAsync();

        return new Base.Response.PageResult<Response.AccountResponse>
        {
            Data = accounts,
            TotalItems = totalItems,
            PageSize = pageSize,
            PageIndex = pageIndex
        };
    }

    public async Task<Response.AccountResponse?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Accounts
            .AsNoTracking()
            .Where(account => account.Id == id && !account.IsDeleted)
            .Select(account => new Response.AccountResponse
            {
                AccountId = account.Id,
                Username = account.Username,

                FullName =
                    account.Member != null ? account.Member.FullName :
                    account.Coach != null ? account.Coach.FullName :
                    account.Receptionist != null ? account.Receptionist.FullName :
                    account.CenterManager != null ? account.CenterManager.FullName :
                    string.Empty,

                Email = account.Email,
                Role = account.Role,
                Status = account.Status,
                CreatedAt = account.CreatedAt,
                UpdatedAt = account.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        Request.UpdateAccountRequest request)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(account =>
                account.Id == id &&
                !account.IsDeleted);

        if (account == null)
        {
            return false;
        }

        var username = request.Username.Trim();
        var email = request.Email.Trim();

        var usernameExists = await _dbContext.Accounts.AnyAsync(other =>
            other.Id != id &&
            other.Username == username);
        if (usernameExists)
        {
            throw new InvalidOperationException("Username đã được sử dụng.");
        }

        var emailExists = await _dbContext.Accounts.AnyAsync(other =>
            other.Id != id &&
            other.Email == email);
        if (emailExists)
        {
            throw new InvalidOperationException("Email đã được sử dụng.");
        }

        account.Username = username;
        account.Email = email;
        account.Status = request.Status;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(account =>
                account.Id == id &&
                !account.IsDeleted);

        if (account == null)
        {
            return false;
        }

        account.IsDeleted = true;
        account.Status = AccountStatus.Inactive;

        await _dbContext.SaveChangesAsync();
        return true;
    }
}
