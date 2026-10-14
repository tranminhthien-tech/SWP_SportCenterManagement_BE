using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Until;

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

    // public async Task<bool> UpdateAsync(
    //     Guid id,
    //     Request.UpdateAccountRequest request)
    // {
    //     var account = await _dbContext.Accounts
    //         .FirstOrDefaultAsync(account =>
    //             account.Id == id &&
    //             !account.IsDeleted);
    //
    //     if (account == null)
    //     {
    //         return false;
    //     }
    //
    //     var username = request.Username.Trim();
    //     var email = request.Email.Trim();
    //
    //     var usernameExists = await _dbContext.Accounts.AnyAsync(other =>
    //         other.Id != id &&
    //         other.Username == username);
    //     if (usernameExists)
    //     {
    //         throw new InvalidOperationException("Username đã được sử dụng.");
    //     }
    //
    //     var emailExists = await _dbContext.Accounts.AnyAsync(other =>
    //         other.Id != id &&
    //         other.Email == email);
    //     if (emailExists)
    //     {
    //         throw new InvalidOperationException("Email đã được sử dụng.");
    //     }
    //
    //     account.Username = username;
    //     account.Email = email;
    //     account.Status = request.Status;
    //
    //     await _dbContext.SaveChangesAsync();
    //     return true;
    // }
    
    public async Task<bool> UpdateAsync(
        Guid id,
        Request.UpdateAccountRequest request)
    {
        var account = await _dbContext.Accounts
            .Include(account => account.Member)
            .Include(account => account.Coach)
            .Include(account => account.Receptionist)
            .Include(account => account.CenterManager)
            .FirstOrDefaultAsync(account =>
                account.Id == id &&
                !account.IsDeleted);

        if (account == null)
        {
            return false;
        }

        // Validate input
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var fullName = request.FullName?.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email không được để trống.");
        }

        var usernameExists = await _dbContext.Accounts.AnyAsync(other =>
            other.Id != id &&
            !other.IsDeleted &&
            other.Username == username);

        if (usernameExists)
        {
            throw new InvalidOperationException(
                "Username đã được sử dụng.");
        }

        var emailExists = await _dbContext.Accounts.AnyAsync(other =>
            other.Id != id &&
            !other.IsDeleted &&
            other.Email == email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "Email đã được sử dụng.");
        }

        // Update account information
        account.Username = username;
        account.Email = email;
        account.Status = request.Status;

        // Update profile FullName according to the linked profile.
        // The Account navigation properties must be configured correctly.
        
        
        if (fullName != null)
        {
            switch (account.Role)
            {
                case AccountRole.Member:
                {
                    if (account.Member != null)
                    {
                        account.Member.FullName = fullName;
                    }
                    else
                    {
                        var member = new Repository.Entity.Member()
                        {
                            AccountId = account.Id,
                            FullName = fullName
                        };

                        _dbContext.Members.Add(member);
                        account.Member = member;
                    }

                    break;
                }

                case AccountRole.Coach:
                {
                    if (account.Coach != null)
                        account.Coach.FullName = fullName;

                    break;
                }

                case AccountRole.Receptionist:
                {
                    if (account.Receptionist != null)
                        account.Receptionist.FullName = fullName;

                    break;
                }

                case AccountRole.CenterManager:
                {
                    if (account.CenterManager != null)
                        account.CenterManager.FullName = fullName;

                    break;
                }
            }
        }


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
    
    
public async Task<Response.AccountResponse> CreateAsync(
    Request.CreateAccountRequest request)
{
    var username = request.Username.Trim();
    var email = request.Email.Trim();
    var fullName = request.FullName.Trim();

    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Username không được để trống.");

    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email không được để trống.");

    if (string.IsNullOrWhiteSpace(fullName))
        throw new ArgumentException("FullName không được để trống.");

    if (string.IsNullOrWhiteSpace(request.Password))
        throw new ArgumentException("Mật khẩu không được để trống.");

    if (!Enum.IsDefined(typeof(AccountRole), request.Role))
        throw new ArgumentException("Role không hợp lệ.");

    if (!Enum.IsDefined(typeof(AccountStatus), request.Status))
        throw new ArgumentException("Status không hợp lệ.");

    var usernameExists = await _dbContext.Accounts.AnyAsync(a =>
        !a.IsDeleted && a.Username == username);

    if (usernameExists)
        throw new InvalidOperationException(
            "Username đã được sử dụng.");

    var emailExists = await _dbContext.Accounts.AnyAsync(a =>
        !a.IsDeleted && a.Email == email);

    if (emailExists)
        throw new InvalidOperationException(
            "Email đã được sử dụng.");

    var account = new Account() 
    {
        Username = username,
        Email = email,
        Password = Argon2Hasher.HashPassword(request.Password),
        Role = request.Role,
        Status = request.Status
    };

    _dbContext.Accounts.Add(account);

    switch (request.Role)
    {
        case AccountRole.Member:
        {
            var member = new Repository.Entity.Member()
            {
                Account = account,
                FullName = fullName,
                Phone = string.Empty,
                Avatar = string.Empty,
                Gender = string.Empty,
                TrainingGoal = string.Empty,

                // Giá trị tạm thời vì form hiện chưa có ngày sinh.
                // Nên bổ sung Dob vào form tạo Member.
                Dob = DateTime.UtcNow.Date
            };

            _dbContext.Members.Add(member);
            break;
        }

        case AccountRole.Coach:
        {
            var coach = new Repository.Entity.Coach()
            {
                Account = account,
                FullName = fullName,
                Phone = string.Empty,
                Avatar = string.Empty,
                Specialization = string.Empty,
                ExperienceYears = 0
            };

            _dbContext.Coaches.Add(coach);
            break;
        }

        case AccountRole.Receptionist:
        {
            var receptionist = new Repository.Entity.Receptionist()
            {
                Account = account,
                FullName = fullName,
                Phone = string.Empty,
                WorkingShift = string.Empty
            };

            _dbContext.Receptionists.Add(receptionist);
            break;
        }

        case AccountRole.CenterManager:
        {
            var manager = new Repository.Entity.CenterManager()
            {
                Account = account,
                FullName = fullName,
                Phone = string.Empty
            };

            _dbContext.CenterManagers.Add(manager);
            break;
        }

        default:
            throw new ArgumentException("Role không được hỗ trợ.");
    }

    await _dbContext.SaveChangesAsync();

    return new Response.AccountResponse
    {
        AccountId = account.Id,
        Username = account.Username,
        Email = account.Email,
        FullName = fullName,
        Role = account.Role,
        Status = account.Status,
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt
    };
}

}
