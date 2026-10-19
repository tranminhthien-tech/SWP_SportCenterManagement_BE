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
                            .Select(a => (AccountStatus?)a.Status)
                            .FirstOrDefaultAsync(
                                context.HttpContext.RequestAborted);

                        if (accountStatus is null)
                        {
                            context.Fail("Account no longer exists.");
                            return;
                        }

                        if (accountStatus != AccountStatus.Active)
                        {
                            context.Fail("Account is not active.");
                        }
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(CenterManagerPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.CenterManager)));

            options.AddPolicy(MemberPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.Member)));

            options.AddPolicy(CoachPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.Coach)));

            options.AddPolicy(ReceptionistPolicy, policy =>
                policy.RequireRole(nameof(AccountRole.Receptionist)));
        });
    }
}
