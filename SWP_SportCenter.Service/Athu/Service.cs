using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Jwt;
using SWP_SportCenter.Service.Until;

namespace SWP_SportCenter.Service.Athu;

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
    public async Task<Response.IdentityResponse> Login(Request.LoginRequest request)
    {
        // Tìm tài khoản bằng Email
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a =>
                a.Email == request.Email &&
                !a.IsDeleted);

        if (account == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password");
        }

        // Kiểm tra mật khẩu bằng Argon2
        bool isPasswordValid = Argon2Hasher.VerifyHash(
            request.Password,
            account.Password
        );

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password");
        }

        // Kiểm tra trạng thái tài khoản
        if (account.Status != AccountStatus.Active)
        {
            throw new UnauthorizedAccessException(
                "Account is not active");
        }


        // Tạo claims cho JWT
        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                account.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                account.Username),

            new Claim(
                ClaimTypes.Email,
                account.Email),

            new Claim(
                ClaimTypes.Role,
                account.Role.ToString())
        };

        // Tạo access token
        var token = _jwtService.GenerateAccessToken(claims);

        // Trả kết quả đăng nhập
        var result = new Response.IdentityResponse
        {
            Access_token = token,
            UserId = account.Id,

        };

        return result;
    }

      // POST /api/auth/register
    public async Task<Response.RegisterResponse> RegisterAsync(
        Request.RegisterRequest request)
    {
        // Kiểm tra username đã tồn tại chưa
        bool usernameExists = await _dbContext.Accounts
            .AnyAsync(a => a.Username == request.Username);

        if (usernameExists)
        {
            throw new InvalidOperationException(
                "Username already exists");
        }

        // Kiểm tra email đã tồn tại chưa
        bool emailExists = await _dbContext.Accounts
            .AnyAsync(a => a.Email == request.Email);

        if (emailExists)
        {
            throw new InvalidOperationException(
                "Email already exists");
        }

        // Hash mật khẩu bằng Argon2
        string hashedPassword =
            Argon2Hasher.HashPassword(request.Password);

        // Tạo Account
        var account = new Account()
        {
            Username = request.Username,
            Email = request.Email,
            Password = hashedPassword,
            Role = AccountRole.Member,
            Status = AccountStatus.Active
        };

        _dbContext.Accounts.Add(account);

        // TODO: Tạo Member profile sau khi xác nhận
        // các thuộc tính bắt buộc trong entity Member.

        await _dbContext.SaveChangesAsync();

        return new Response.RegisterResponse
        {
            UserId = account.Id,
            Email = account.Email,
            Message = "Registration successful"
        };
    }

    // POST /api/auth/login
    public async Task<Response.IdentityResponse> LoginAsync(
        Request.LoginRequest request)
    {
        var account = await _dbContext.Accounts
            .FirstOrDefaultAsync(a =>
                a.Email == request.Email &&
                !a.IsDeleted);

        if (account == null)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password");
        }

        // Verify mật khẩu bằng Argon2
        bool isPasswordValid = Argon2Hasher.VerifyHash(
            request.Password,
            account.Password);

        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password");
        }

        if (account.Status != AccountStatus.Active)
        {
            throw new UnauthorizedAccessException(
                "Account is not active");
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                account.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                account.Username),

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
}
