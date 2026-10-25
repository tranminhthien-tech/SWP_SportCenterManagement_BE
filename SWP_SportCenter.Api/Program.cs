using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Service.Dashboard;
using SWP_SportCenter.Service.Notification;
using SWP_SportCenter.Service.Accounts;
using SWP_SportCenter.Service.Attendance;
using SWP_SportCenter.Service.AuditLog;
using SWP_SportCenter.Service.CenterManager;
using SWP_SportCenter.Service.ClassBooking;
using SWP_SportCenter.Service.Classes;
using SWP_SportCenter.Service.ClassSession;
using SWP_SportCenter.Service.Coach;
using SWP_SportCenter.Service.Invoice;
using SWP_SportCenter.Service.Jwt;
using SWP_SportCenter.Service.Receptionist;
using SWP_SportCenter.Service.Room;
using SWP_SportCenter.Service.SportCategory;
using SWP_SportCenter.Service.Until;
using RegisterService = SWP_SportCenter.Service.Register;
using MemberService = SWP_SportCenter.Service.Member;
using MembershipPackageService = SWP_SportCenter.Service.MembershipPackage;
using MembershipService = SWP_SportCenter.Service.Membership;
using TrainingPlanService = SWP_SportCenter.Service.TrainingPlan;
using TrainingResultService = SWP_SportCenter.Service.TrainingResult;

using CloudService = SWP_SportCenter.Service.CloudinaryService;

using MediaService = SWP_SportCenter.Service.MediaService;

Env.Load();

var aspnetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspnetCoreEnv);
var builder = WebApplication.CreateBuilder(args);
// FIX B12: fail fast nếu chưa cấu hình kết nối qua env/user-secrets.
if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured through environment variables or user-secrets.");

// CODE CŨ: log hash mật khẩu demo khi khởi động.
// FIX B12: không xuất thông tin xác thực ra log.

// CODE CŨ: builder.Services.AddControllers();
// FIX B19: áp dụng giới hạn phân trang cho mọi action có pageSize/pageIndex.
builder.Services.AddControllers(o => o.Filters.Add<SWP_SportCenter.Filters.PaginationFilter>());
builder.Services.AddHttpContextAccessor();
// FIX B4 B5 B7 B15: dùng chung cách lấy Account/Member từ token.
builder.Services.AddScoped<SWP_SportCenter.Service.Security.CurrentActor>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerServices();
builder.Services.AddJwtServices(builder.Configuration);

// CODE CŨ: AddDbContext<AppDbContext>(options => options.UseNpgsql(...));
// FIX B33: đăng ký interceptor ở server, không phụ thuộc FE gửi audit.
builder.Services.AddScoped<SWP_SportCenter.Infrastructure.ServerAuditInterceptor>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ).AddInterceptors(services.GetRequiredService<SWP_SportCenter.Infrastructure.ServerAuditInterceptor>())
);

builder.Services.AddScoped<MemberService.IService, MemberService.Service>();
builder.Services.AddScoped<MembershipService.IService, MembershipService.Service>();
builder.Services.AddScoped<MembershipPackageService.IService, MembershipPackageService.Service>();
builder.Services.AddScoped<TrainingPlanService.IService, TrainingPlanService.Service>();
builder.Services.AddScoped<TrainingResultService.IService, TrainingResultService.Service>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ICoachService, CoachService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ISportCategoryService, SportCategoryService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IClassSessionService, ClassSessionService>();
builder.Services.AddScoped<IClassBookingService, ClassBookingService>();
builder.Services.AddScoped<ICenterManagerService, CenterManagerService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IReceptionistService, ReceptionistService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<MediaService.IService,CloudService.CloudinaryService>();
// FIX B31 B32: API hỗ trợ và job nhắc hạn chạy tại BE.
builder.Services.AddScoped<SWP_SportCenter.Service.Support.SupportRequestService>();
builder.Services.AddScoped<MembershipExpiryNotifier>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddHostedService<SWP_SportCenter.Infrastructure.MembershipExpiryWorker>();
builder.Services.AddScoped<RegisterService.IService, RegisterService.Service>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Payment.IPaymentService, SWP_SportCenter.Service.Payment.PaymentService>();

var allowedOrigins = builder.Configuration
                         .GetSection("Cors:AllowedOrigins")
                         .Get<string[]>()
                     ?? new[]
                     {
                         "http://localhost:5173",
                         "http://127.0.0.1:5173",
                         "https://swpsport-center-managementfe.vercel.app"
                     };

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontEnd", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// FIX B1: opt-in rõ ràng tại Render; áp dụng migration trước khi nhận request.
// Không tự chạy ở local hoặc khi biến Database__ApplyMigrations chưa bật.
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

// FIX B6: đặt trước middleware khác để lỗi runtime trả ProblemDetails JSON.
app.UseMiddleware<SWP_SportCenter.Middleware.ApiExceptionMiddleware>();

app.UseSwaggerAPI();
app.UseCors("FrontEnd"); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
