using Microsoft.EntityFrameworkCore;
using Npgsql;
using SWP_SportCenter.Service.Exceptions;
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
        int pageIndex,
        bool? hasProfile = null)
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

        // FIX 2026-10-22: lọc trước phân trang, đối chiếu đúng bảng hồ sơ theo role.
        if (hasProfile.HasValue)
        {
            query = query.Where(a => (
                (a.Role == AccountRole.Member && a.Member != null) ||
                (a.Role == AccountRole.Coach && a.Coach != null) ||
                (a.Role == AccountRole.Receptionist && a.Receptionist != null) ||
                (a.Role == AccountRole.CenterManager && a.CenterManager != null)
            ) == hasProfile.Value);
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
                // FIX: trạng thái hồ sơ phục vụ danh sách/dropdown.
                HasProfile =
                    (account.Role == AccountRole.Member && account.Member != null) ||
                    (account.Role == AccountRole.Coach && account.Coach != null) ||
                    (account.Role == AccountRole.Receptionist && account.Receptionist != null) ||
                    (account.Role == AccountRole.CenterManager && account.CenterManager != null),

                // CODE CŨ: lấy FullName từ profile, trả rỗng khi profile bị xóa.
                // FIX: đọc bản thông tin cá nhân độc lập trong Account.
                FullName = account.FullName,

                // FIX 2026-10-22: map phone từ đúng profile theo role.
                // CODE CŨ: lấy Phone từ profile liên kết.
                // FIX: Phone vẫn còn sau khi xóa profile.
                Phone = account.Phone,

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
                // FIX: GET chi tiết cũng phản ánh trạng thái hồ sơ.
                HasProfile =
                    (account.Role == AccountRole.Member && account.Member != null) ||
                    (account.Role == AccountRole.Coach && account.Coach != null) ||
                    (account.Role == AccountRole.Receptionist && account.Receptionist != null) ||
                    (account.Role == AccountRole.CenterManager && account.CenterManager != null),

                // CODE CŨ: đọc tên từ profile liên kết.
                // FIX: thống nhất GET chi tiết với GET danh sách.
                FullName = account.FullName,

                // FIX 2026-10-22: GET /api/accounts/{id} cũng trả phone.
                // CODE CŨ: đọc phone từ profile liên kết.
                // FIX: dùng thông tin lưu ở Account.
                Phone = account.Phone,

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
        // CODE CŨ: var fullName = request.FullName?.Trim(); var phone = request.Phone?.Trim();
        // FIX: chuẩn hóa giá trị thiếu; nhân sự chưa có profile được phép để trống.
        var fullName = request.FullName?.Trim() ?? string.Empty;
        var phone = request.Phone?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email không được để trống.");
        }

        // FIX 2026-10-22: FullName bắt buộc để phone luôn được cập nhật cùng profile.
        // FIX: cho phép sửa tài khoản nhân sự chưa có profile mà không cần tên/phone.
        var needsProfileDetails = account.Role == AccountRole.Member || account.Coach != null ||
            account.Receptionist != null || account.CenterManager != null;
        // CODE CŨ: if (string.IsNullOrWhiteSpace(fullName))
        if (needsProfileDetails && string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("FullName không được để trống.");
        }

        // CODE CŨ: if (string.IsNullOrWhiteSpace(phone))
        if (needsProfileDetails && string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone không được để trống.");
        }

        if (phone.Length > 20)
        {
            throw new ArgumentException("Phone tối đa 20 ký tự.");
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

        // FIX 2026-10-22: mỗi bảng profile có unique index cho phone.
        var phoneExists = needsProfileDetails && (account.Role switch
        {
            AccountRole.Member => await _dbContext.Members.AnyAsync(x =>
                x.AccountId != id && x.Phone == phone),
            AccountRole.Coach => await _dbContext.Coaches.AnyAsync(x =>
                x.AccountId != id && x.Phone == phone),
            AccountRole.Receptionist => await _dbContext.Receptionists.AnyAsync(x =>
                x.AccountId != id && x.Phone == phone),
            AccountRole.CenterManager => await _dbContext.CenterManagers.AnyAsync(x =>
                x.AccountId != id && x.Phone == phone),
            _ => false
        });

        if (phoneExists)
        {
            throw new InvalidOperationException("Phone đã được sử dụng.");
        }

        // Update account information
        account.Username = username;
        account.Email = email;
        account.Status = request.Status;

        // FIX: lưu thông tin độc lập cả khi tài khoản nhân sự chưa có profile.
        account.FullName = fullName;
        account.Phone = phone;

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
                        // FIX 2026-10-22: lưu phone vào Member profile.
                        account.Member.Phone = phone;
                    }
                    else
                    {
                        // CODE CŨ: chỉ gán AccountId và FullName, nên có thể tạo profile thiếu dữ liệu bắt buộc.
                        // FIX 2026-10-22: luôn tạo Member profile hợp lệ khi gặp Member cũ chưa có profile.
                        var member = new Repository.Entity.Member()
                        {
                            Id = Guid.NewGuid(),
                            AccountId = account.Id,
                            FullName = fullName,
                            Dob = DateTime.UtcNow.Date,
                            Gender = string.Empty,
                            Avatar = string.Empty,
                            TrainingGoal = string.Empty
                        };

                        // CODE CŨ: member.Phone = $"pending-{member.Id:N}"[..20];
                        // FIX 2026-10-22: dùng phone do FE gửi lên.
                        member.Phone = phone;

                        _dbContext.Members.Add(member);
                        account.Member = member;
                    }

                    break;
                }

                case AccountRole.Coach:
                {
                    if (account.Coach != null)
                    {
                        account.Coach.FullName = fullName;
                        // FIX 2026-10-22: lưu phone vào Coach profile.
                        account.Coach.Phone = phone;
                    }

                    break;
                }

                case AccountRole.Receptionist:
                {
                    if (account.Receptionist != null)
                    {
                        account.Receptionist.FullName = fullName;
                        // FIX 2026-10-22: lưu phone vào Receptionist profile.
                        account.Receptionist.Phone = phone;
                    }

                    break;
                }

                case AccountRole.CenterManager:
                {
                    if (account.CenterManager != null)
                    {
                        account.CenterManager.FullName = fullName;
                        // FIX 2026-10-22: lưu phone vào CenterManager profile.
                        account.CenterManager.Phone = phone;
                    }

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
    // CODE CŨ: var fullName = request.FullName.Trim(); var phone = request.Phone.Trim();
    // FIX: thông tin hồ sơ nhân sự được nhập sau, chỉ Member tạo hồ sơ ngay.
    var fullName = request.FullName?.Trim() ?? string.Empty;
    var phone = request.Phone?.Trim() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Username không được để trống.");

    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email không được để trống.");

    // CODE CŨ: bắt buộc FullName cho mọi role.
    if (request.Role == AccountRole.Member && string.IsNullOrWhiteSpace(fullName))
        throw new BusinessException("FullName không được để trống.");

    // CODE CŨ: bắt buộc Phone cho mọi role.
    if (request.Role == AccountRole.Member && string.IsNullOrWhiteSpace(phone))
        throw new BusinessException("Phone không được để trống.");

    if (phone.Length > 20)
        throw new ArgumentException("Phone tối đa 20 ký tự.");

    if (string.IsNullOrWhiteSpace(request.Password))
        throw new ArgumentException("Mật khẩu không được để trống.");

    if (!Enum.IsDefined(typeof(AccountRole), request.Role))
        throw new ArgumentException("Role không hợp lệ.");

    if (!Enum.IsDefined(typeof(AccountStatus), request.Status))
        throw new ArgumentException("Status không hợp lệ.");

    // FIX 2026-10-22: kiểm tra phone trước khi tạo profile để không vướng unique index DB.
    // CODE CŨ: kiểm tra phone theo cả bốn bảng profile khi tạo Account.
    // FIX: chỉ tạo Member profile; nhân sự kiểm tra phone tại API tạo hồ sơ riêng.
    var phoneExists = request.Role == AccountRole.Member &&
        await _dbContext.Members.AnyAsync(x => x.Phone == phone);

    if (phoneExists)
        // CODE CŨ: throw new InvalidOperationException("Phone đã được sử dụng.");
        // FIX 2026-10-22: trả lỗi nghiệp vụ 400 JSON qua Controller.
        throw new BusinessException("Phone đã được sử dụng.");

    // CODE CŨ: kiểm tra !a.IsDeleted && a.Username == username.
    // FIX 2026-10-22: unique index áp dụng cả Account đã xóa mềm.
    var usernameExists = await _dbContext.Accounts.AnyAsync(a =>
        a.Username == username);

    if (usernameExists)
        // CODE CŨ: throw new InvalidOperationException("Username đã được sử dụng.");
        // FIX 2026-10-22: dùng lỗi nghiệp vụ riêng.
        throw new BusinessException(
            "Username đã được sử dụng.");

    // CODE CŨ: kiểm tra !a.IsDeleted && a.Email == email.
    // FIX 2026-10-22: kiểm tra email trên toàn bộ Account theo unique index DB.
    var emailExists = await _dbContext.Accounts.AnyAsync(a =>
        a.Email == email);

    if (emailExists)
        // CODE CŨ: throw new InvalidOperationException("Email đã được sử dụng.");
        // FIX 2026-10-22: dùng lỗi nghiệp vụ riêng.
        throw new BusinessException(
            "Email đã được sử dụng.");

    var account = new Account() 
    {
        Username = username,
        Email = email,
        // FIX: tạo Account lưu tên/phone, không cần tạo hồ sơ nhân sự ngầm.
        FullName = fullName,
        Phone = phone,
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
                // CODE CŨ: Phone = string.Empty,
                // FIX 2026-10-22: lưu phone do FE gửi lên.
                Phone = phone,
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
            /* CODE CŨ: tự tạo Coach profile khi tạo Account.
            var coach = new Repository.Entity.Coach()
            {
                Account = account,
                FullName = fullName,
                // CODE CŨ: Phone = string.Empty,
                // FIX 2026-10-22: lưu phone do FE gửi lên.
                Phone = phone,
                Avatar = string.Empty,
                Specialization = string.Empty,
                ExperienceYears = 0
            };

            _dbContext.Coaches.Add(coach);
            */
            // FIX 2026-10-22: chỉ POST /api/coaches mới tạo Coach profile.
            break;
        }

        case AccountRole.Receptionist:
        {
            /* CODE CŨ: tự tạo Receptionist profile khi tạo Account.
            var receptionist = new Repository.Entity.Receptionist()
            {
                Account = account,
                FullName = fullName,
                // CODE CŨ: Phone = string.Empty,
                // FIX 2026-10-22: lưu phone do FE gửi lên.
                Phone = phone,
                WorkingShift = string.Empty
            };

            _dbContext.Receptionists.Add(receptionist);
            */
            // FIX 2026-10-22: hồ sơ lễ tân được tạo ở API riêng.
            break;
        }

        case AccountRole.CenterManager:
        {
            /* CODE CŨ: tự tạo CenterManager profile khi tạo Account.
            var manager = new Repository.Entity.CenterManager()
            {
                Account = account,
                FullName = fullName,
                // CODE CŨ: Phone = string.Empty
                // FIX 2026-10-22: lưu phone do FE gửi lên.
                Phone = phone
            };

            _dbContext.CenterManagers.Add(manager);
            */
            // FIX 2026-10-22: hồ sơ quản lý được tạo ở API riêng.
            break;
        }

        default:
            throw new ArgumentException("Role không được hỗ trợ.");
    }

    // CODE CŨ: await _dbContext.SaveChangesAsync();
    // FIX 2026-10-22: bắt unique violation kể cả khi hai request cùng vượt qua kiểm tra trước.
    try
    {
        await _dbContext.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (
        ex.InnerException is PostgresException postgres &&
        postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
        postgres.ConstraintName is "IX_account_username" or "IX_account_email" or
            "IX_member_phone" or "IX_coach_phone" or "IX_receptionist_phone" or "IX_center_manager_phone")
    {
        var constraint = ((PostgresException)ex.InnerException!).ConstraintName;
        throw new BusinessException(constraint switch
        {
            "IX_account_username" => "Username đã được sử dụng.",
            "IX_account_email" => "Email đã được sử dụng.",
            _ => "Phone đã được sử dụng."
        });
    }

    return new Response.AccountResponse
    {
        AccountId = account.Id,
        Username = account.Username,
        Email = account.Email,
        // CODE CŨ: FullName = fullName,
        // FIX: phản hồi đúng dữ liệu đã lưu; nhân sự chưa có profile thì để trống.
        // CODE CŨ: FullName = request.Role == AccountRole.Member ? fullName : string.Empty,
        // FIX: trả thông tin cá nhân được lưu ở Account.
        FullName = account.FullName,
        // FIX 2026-10-22: POST /api/accounts trả phone vừa lưu.
        // CODE CŨ: Phone = phone,
        // FIX: chỉ Member có profile ngay sau khi tạo Account.
        // CODE CŨ: Phone = request.Role == AccountRole.Member ? phone : string.Empty,
        // FIX: phản hồi phone độc lập với profile.
        Phone = account.Phone,
        HasProfile = request.Role == AccountRole.Member,
        Role = account.Role,
        Status = account.Status,
        CreatedAt = account.CreatedAt,
        UpdatedAt = account.UpdatedAt
    };
}

}
