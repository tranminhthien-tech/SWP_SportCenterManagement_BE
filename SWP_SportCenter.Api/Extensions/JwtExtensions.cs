namespace SWP_SportCenter.Extensions;


using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Enum;


public static class JwtExtensions
{
    public const string CenterManagerPolicy = "CenterManagerPolicy";
    public const string MemberPolicy = "MemberPolicy";
    public const string CoachPolicy = "CoachPolicy";
    public const string ReceptionistPolicy = "ReceptionistPolicy";
    public const string ManagerOrReceptionistPolicy = "ManagerOrReceptionistPolicy";
    public const string ManagerOrCoachPolicy = "ManagerOrCoachPolicy";
    public const string StaffPolicy = "StaffPolicy";

    public static void AddJwtServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("JwtOptions");

        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException(
                "JwtOptions:SecretKey is not configured.");

        var issuer = jwtSection["Issuer"]
            ?? throw new InvalidOperationException(
                "JwtOptions:Issuer is not configured.");

        var audience = jwtSection["Audience"]
            ?? throw new InvalidOperationException(
                "JwtOptions:Audience is not configured.");

        var key = Encoding.UTF8.GetBytes(secretKey);
        // FIX B12: không chạy với secret thiếu/yếu sau khi chuyển sang env.
        if (key.Length < 32) throw new InvalidOperationException("JwtOptions:SecretKey must contain at least 32 bytes.");

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,

                        ValidIssuer = issuer,
                        ValidAudience = audience,

                        IssuerSigningKey =
                            new SymmetricSecurityKey(key),

                        // JWT phải chứa claim NameIdentifier là AccountId.
                        NameClaimType = ClaimTypes.NameIdentifier,

                        // Đọc role từ claim Role.
                        RoleClaimType = ClaimTypes.Role,

                        ClockSkew = TimeSpan.FromMinutes(1)
                    };

                options.Events = new JwtBearerEvents
                {
                    // Hỗ trợ JWT cho SignalR NotificationHub.
                    OnMessageReceived = context =>
                    {
                        var accessToken =
                            context.Request.Query["access_token"];

                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/notificationHub"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },

                    // Kiểm tra Account còn tồn tại và đang hoạt động.
                    OnTokenValidated = async context =>
                    {
                        var accountIdRaw = context.Principal?
                            .FindFirstValue(ClaimTypes.NameIdentifier);

                        if (string.IsNullOrWhiteSpace(accountIdRaw) ||
                            !Guid.TryParse(accountIdRaw, out var accountId))
                        {
                            context.Fail("Invalid token subject.");
                            return;
                        }

                        var dbContext = context.HttpContext
                            .RequestServices
                            .GetRequiredService<AppDbContext>();

                        var accountStatus = await dbContext.Accounts
                            .AsNoTracking()
                            .Where(a => a.Id == accountId && !a.IsDeleted)
                            // CODE CŨ: chỉ lấy Status nên role trong token cũ không bị thu hồi.
                            // .Select(a => (AccountStatus?)a.Status)
                            // FIX B50: đối chiếu cả role hiện tại trong DB.
                            .Select(a => new { a.Status, a.Role })
                            .FirstOrDefaultAsync(
                                context.HttpContext.RequestAborted);

                        if (accountStatus is null)
                        {
                            context.Fail("Account no longer exists.");
                            return;
                        }

                        // CODE CŨ: if (accountStatus != AccountStatus.Active)
                        if (accountStatus.Status != AccountStatus.Active)
                        {
                            context.Fail("Account is not active.");
                        }
                        // FIX B50: chuyển role phải đăng nhập lại để lấy quyền mới.
                        if (context.Principal?.FindFirstValue(ClaimTypes.Role) != accountStatus.Role.ToString())
                            context.Fail("Account role changed. Please sign in again.");
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            // Policy cho từng role
            options.AddPolicy(CenterManagerPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.CenterManager)));

            options.AddPolicy(MemberPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.Member)));

            options.AddPolicy(CoachPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.Coach)));

            options.AddPolicy(ReceptionistPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.Receptionist)));

            // Policy kết hợp: CenterManager hoặc Receptionist
            options.AddPolicy("ManagerOrReceptionistPolicy", policy =>
                policy.RequireRole(
                    nameof(AccountRole.CenterManager),
                    nameof(AccountRole.Receptionist)));

            // Policy kết hợp: CenterManager hoặc Coach
            options.AddPolicy("ManagerOrCoachPolicy", policy =>
                policy.RequireRole(
                    nameof(AccountRole.CenterManager),
                    nameof(AccountRole.Coach)));
            options.AddPolicy(StaffPolicy, policy =>
                policy.RequireRole(
                    nameof(AccountRole.CenterManager),
                    nameof(AccountRole.Coach),
                    nameof(AccountRole.Receptionist)));
            options.AddPolicy("CoachAndReceptionPolicy", policy =>
                policy.RequireRole(
                    nameof(AccountRole.Coach),
                    nameof(AccountRole.Receptionist)));
            options.AddPolicy("AttendanceViewPolicy", policy =>
                policy.RequireRole(
                    nameof(AccountRole.Coach),
                    nameof(AccountRole.Receptionist),
                    nameof(AccountRole.CenterManager)));
        });
    }
}
