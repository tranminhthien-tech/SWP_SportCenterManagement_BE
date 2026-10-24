using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Jwt;
using SWP_SportCenter.Service.Until;
using SWP_SportCenter.Service.Exceptions;

namespace SWP_SportCenter.Service.Register;

public class Service: IService 
{
    
    private readonly IJwtService _jwtService;
    private readonly AppDbContext _dbContext;
    private readonly JwtOptions _jwtOption = new();
    private readonly IHttpContextAccessor _httpContextAccessor;

    public Service(IJwtService jwtService, AppDbContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _jwtService = jwtService;
        _dbContext = dbContext;
        configuration.GetSection(nameof(JwtOptions)).Bind(_jwtOption);
        _httpContextAccessor = httpContextAccessor;
    }
    // public async Task<Response.IdentityResponse> Login(Request.LoginRequest request)
    // {
    //     // Tìm tài khoản bằng Email
    //     var account = await _dbContext.Accounts
    //         .FirstOrDefaultAsync(a =>
    //             a.Email == request.Email &&
    //             !a.IsDeleted);
    //
    //     if (account == null)
    //     {
    //         throw new UnauthorizedAccessException(
    //             "Invalid email or password");
    //     }
    //
    //     // Kiểm tra mật khẩu bằng Argon2
    //     bool isPasswordValid = Argon2Hasher.VerifyHash(
    //         request.Password,
    //         account.Password
    //     );
    //
    //     if (!isPasswordValid)
    //     {
    //         throw new UnauthorizedAccessException(
    //             "Invalid email or password");
    //     }
    //
    //     // Kiểm tra trạng thái tài khoản
    //     if (account.Status != AccountStatus.Active)
    //     {
    //         throw new UnauthorizedAccessException(
    //             "Account is not active");
    //     }
    //
    //
    //     // Tạo claims cho JWT
    //     var claims = new List<Claim>
    //     {
    //         new Claim(
    //             ClaimTypes.NameIdentifier,
    //             account.Id.ToString()),
    //
    //         new Claim(
    //             ClaimTypes.Name,
    //             account.Username),
    //
    //         new Claim(
    //             ClaimTypes.Email,
    //             account.Email),
    //
    //         new Claim(
    //             ClaimTypes.Role,
    //             account.Role.ToString())
    //     };
    //
    //     // Tạo access token
    //     var token = _jwtService.GenerateAccessToken(claims);
    //
    //     // Trả kết quả đăng nhập
    //     var result = new Response.IdentityResponse
    //     {
    //         Access_token = token,
    //         UserId = account.Id,
    //
    //     };
    //
    //     return result;
    // }
    public async Task<Response.IdentityResponse> Login(
        Request.LoginRequest request)
    {
        var account = await _dbContext.Accounts
            .Include(a => a.Member)
            .Include(a => a.Coach)
            .Include(a => a.Receptionist)
            .Include(a => a.CenterManager)
            .FirstOrDefaultAsync(a =>
                (a.Username == request.Email ||
                 a.Email == request.Email) &&
                !a.IsDeleted);

        if (account == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid username or password");
        }

        bool isPasswordValid = Argon2Hasher.VerifyHash(
            request.Password,
            account.Password);

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid username or password");
        }

        if (account.Status != AccountStatus.Active)
        {
            throw new UnauthorizedAccessException(
                "Account is not active");
        }

        var fullName = account.Role switch
        {
            AccountRole.Member => account.Member?.FullName,
            AccountRole.Coach => account.Coach?.FullName,
            AccountRole.Receptionist => account.Receptionist?.FullName,
            AccountRole.CenterManager => account.CenterManager?.FullName,
            _ => null
        };

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                account.Id.ToString()),

            // Username dùng cho định danh hiện tại
            new Claim(
                ClaimTypes.Name,
                account.Username),

            // FullName chỉ dùng để hiển thị
            new Claim(
                "FullName",
                fullName ?? string.Empty),

            new Claim(
                ClaimTypes.Email,
                account.Email),

            new Claim(
                ClaimTypes.Role,
                account.Role.ToString())
        };

        string token = _jwtService.GenerateAccessToken(claims);

        return new Response.IdentityResponse
        {
            Access_token = token,
            UserId = account.Id,
        };
    }


      // POST /api/auth/register
    public async Task<Response.RegisterResponse> RegisterAsync(
        Request.RegisterRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        // FIX B3: validate cả khi Service được gọi ngoài MVC.
        System.ComponentModel.DataAnnotations.Validator.ValidateObject(request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request), true);
        var phone = request.Phone.Trim();
        if (await _dbContext.Members.AnyAsync(m => m.Phone == phone))
            throw new SWP_SportCenter.Service.Exceptions.ConflictException("Số điện thoại này đã được sử dụng.");

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username không được để trống");

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email không được để trống");

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("FullName không được để trống");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Mật khẩu không được để trống");

        // Kiểm tra username đã tồn tại chưa
        bool usernameExists = await _dbContext.Accounts
            .AnyAsync(a => a.Username == username);
    
        if (usernameExists)
        {
            // CODE CŨ: throw new InvalidOperationException(
            //                 "Username already exists");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException(
                "Username already exists");
        }
    
        // Kiểm tra email đã tồn tại chưa
        bool emailExists = await _dbContext.Accounts
            .AnyAsync(a => a.Email == email);
    
        if (emailExists)
        {
            // CODE CŨ: throw new InvalidOperationException(
            //                 "Email already exists");
            // FIX B6: phân loại lỗi nghiệp vụ để middleware trả JSON 400/409.
            throw new SWP_SportCenter.Service.Exceptions.ConflictException(
                "Email already exists");
        }
    
        // Hash mật khẩu bằng Argon2
        string hashedPassword =
            Argon2Hasher.HashPassword(request.Password);
    
        // Tạo Account
        var account = new Account()
        {
            Username = username,
            Email = email,
            Password = hashedPassword,
            Role = AccountRole.Member,
            Status = AccountStatus.Active
        };

        // CODE CŨ: _dbContext.Accounts.Add(account);
        // CODE CŨ: await _dbContext.SaveChangesAsync();
        // Code cũ chỉ lưu Account nên Member đăng ký không có Member profile.

        // FIX 2026-10-22: lưu Account và Member trong cùng SaveChangesAsync (một transaction EF Core).
        _dbContext.Accounts.Add(account);

        var member = new Repository.Entity.Member
        {
            // FIX: tạo Id trước để sinh số điện thoại placeholder duy nhất, tránh vi phạm unique index.
            Id = Guid.NewGuid(),
            Account = account,
            FullName = fullName,
            // CODE CŨ: Dob = DateTime.UtcNow.Date, Gender = string.Empty, Phone = string.Empty,
            // FIX B3 B18: lưu dữ liệu thật do người dùng cung cấp.
            Dob = DateTime.SpecifyKind(request.Dob.Date, DateTimeKind.Utc),
            Gender = request.Gender?.Trim() ?? string.Empty,
            Phone = phone,
            Avatar = string.Empty,
            // CODE CŨ: TrainingGoal = string.Empty
            // FIX B3: lưu mục tiêu tập luyện.
            TrainingGoal = request.TrainingGoal?.Trim() ?? string.Empty
        };

        // CODE CŨ: member.Phone = $"pending-{member.Id:N}"[..20];
        // FIX B18: không tạo số điện thoại giả.

        _dbContext.Members.Add(member);
        await _dbContext.SaveChangesAsync();
    
        return new Response.RegisterResponse
        {
            UserId = account.Id,
            Username = account.Username,
            Email = account.Email,
            FullName = member.FullName,
            Role = account.Role.ToString(),
            // FIX: không gửi OTP hay email; đăng ký hoàn tất ngay sau khi dữ liệu được lưu.
            Message = "Registration successful. You can log in now."
        };
    }
    
    // POST /api/auth/login
    // public async Task<Response.IdentityResponse> LoginAsync(
    //     Request.LoginRequest request)
    // {
    //     var account = await _dbContext.Accounts
    //         .FirstOrDefaultAsync(a =>
    //             a.Email == request.Email &&
    //             !a.IsDeleted);
    //
    //     if (account == null)
    //     {
    //         throw new UnauthorizedAccessException(
    //             "Invalid email or password");
    //     }
    //
    //     // Verify mật khẩu bằng Argon2
    //     bool isPasswordValid = Argon2Hasher.VerifyHash(
    //         request.Password,
    //         account.Password);
    //
    //     if (!isPasswordValid)
    //     {
    //         throw new UnauthorizedAccessException(
    //             "Invalid email or password");
    //     }
    //
    //     if (account.Status != AccountStatus.Active)
    //     {
    //         throw new UnauthorizedAccessException(
    //             "Account is not active");
    //     }
    //
    //     var claims = new List<Claim>
    //     {
    //         new Claim(
    //             ClaimTypes.NameIdentifier,
    //             account.Id.ToString()),
    //
    //         new Claim(
    //             ClaimTypes.Name,
    //             account.Username),
    //
    //         new Claim(
    //             ClaimTypes.Email,
    //             account.Email),
    //
    //         new Claim(
    //             ClaimTypes.Role,
    //             account.Role.ToString())
    //     };
    //
    //     string token = _jwtService.GenerateAccessToken(claims);
    //
    //     return new Response.IdentityResponse
    //     {
    //         Access_token = token,
    //         UserId = account.Id,
    //     };
    // }
    
    

    // GET /api/auth/me
    public async Task<Response.MeResponse> GetMeAsync()
    {
        var accountIdValue = _httpContextAccessor
            .HttpContext?
            .User
            .FindFirst(ClaimTypes.NameIdentifier)?
            .Value;

        if (!Guid.TryParse(accountIdValue, out Guid accountId))
        {
            throw new UnauthorizedAccessException(
                "Invalid account identity");
        }

        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a =>
                a.Id == accountId &&
                !a.IsDeleted);

        if (account == null)
        {
            throw new UnauthorizedAccessException(
                "Account not found");
        }

        return new Response.MeResponse
        {
            UserId = account.Id,
            Username = account.Username,
            Email = account.Email,
            Role = account.Role.ToString(),
            Status = account.Status.ToString(),
        };
    }

    // POST /api/auth/logout
    public Task<Response.AuthMessageResponse> LogoutAsync()
    {
        // Chưa có cơ chế thu hồi JWT ở server.
        // Client cần xóa Access Token sau khi logout.
        return Task.FromResult(new Response.AuthMessageResponse
        {
            Message = "Logged out. Please remove the access token."
        });
    }

    // Chưa triển khai: cần EmailService và OTP storage
    public Task<Response.AuthMessageResponse> ForgotPasswordAsync(
        Request.ForgotPasswordRequest request)
    {
        throw new NotImplementedException(
            "Forgot password requires OTP and email services.");
    }

    // Chưa triển khai: cần xác minh OTP trước khi đổi mật khẩu
    public Task<Response.AuthMessageResponse> ResetPasswordAsync(
        Request.ResetPasswordRequest request)
    {
        throw new NotImplementedException(
            "Reset password requires OTP verification.");
    }

    // FIX B36: Đổi mật khẩu trong Hồ sơ cá nhân
    public async Task<Response.AuthMessageResponse> ChangePasswordAsync(
        Request.ChangePasswordRequest request)
    {
        var accountIdValue = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(accountIdValue, out Guid accountId))
        {
            throw new UnauthorizedAccessException("Bạn chưa đăng nhập hoặc phiên làm việc đã hết hạn.");
        }

        var account = await _dbContext.Accounts.FirstOrDefaultAsync(a => a.Id == accountId && !a.IsDeleted);
        if (account == null)
        {
            throw new KeyNotFoundException("Tài khoản không tồn tại.");
        }

        // Kiểm tra mật khẩu hiện tại bằng Argon2
        if (!Argon2Hasher.VerifyHash(request.CurrentPassword, account.Password))
        {
            throw new BusinessException("Mật khẩu hiện tại không đúng.");
        }

        // Mật khẩu mới >= 8 ký tự
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
        {
            throw new BusinessException("Mật khẩu mới phải có ít nhất 8 ký tự.");
        }

        // Mật khẩu mới phải khác mật khẩu cũ
        if (request.CurrentPassword == request.NewPassword || Argon2Hasher.VerifyHash(request.NewPassword, account.Password))
        {
            throw new BusinessException("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
        }

        // Băm mật khẩu mới bằng Argon2 và lưu
        account.Password = Argon2Hasher.HashPassword(request.NewPassword);
        account.UpdatedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync();

        return new Response.AuthMessageResponse
        {
            Message = "Đổi mật khẩu thành công."
        };
    }
}