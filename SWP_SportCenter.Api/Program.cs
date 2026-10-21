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

Env.Load();

var aspnetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspnetCoreEnv);
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("========================================");
Console.WriteLine("DEMO PASSWORD HASH:");
Console.WriteLine(Argon2Hasher.HashPassword("123456"));
Console.WriteLine("========================================");

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerServices();
builder.Services.AddJwtServices(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
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
builder.Services.AddScoped<RegisterService.IService, RegisterService.Service>();
builder.Services.AddScoped<IJwtService, JwtService>();

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

app.UseSwaggerAPI();
app.UseCors("FrontEnd"); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();