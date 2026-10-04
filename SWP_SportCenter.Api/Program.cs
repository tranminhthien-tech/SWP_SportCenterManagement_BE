using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;
using DotNetEnv;
using SWP_SportCenter.Service.Until;
using MemberService = SWP_SportCenter.Service.Member;
using MembershipService = SWP_SportCenter.Service.Membership;
using MembershipPackageService = SWP_SportCenter.Service.MembershipPackage;
using TrainingPlanService = SWP_SportCenter.Service.TrainingPlan;
using TrainingResultService = SWP_SportCenter.Service.TrainingResult;
using SWP_SportCenter.Service.Classes;
using SWP_SportCenter.Service.Coach;
using SWP_SportCenter.Service.Room;
using SWP_SportCenter.Service.SportCategory;
using SWP_SportCenter.Service.Invoice;
using SWP_SportCenter.Service.ClassSession;
using SWP_SportCenter.Service.ClassBooking;
using SWP_SportCenter.Service.CenterManager;
using SWP_SportCenter.Service.AuditLog;
using SWP_SportCenter.Service.Attendance;
using SWP_SportCenter.Service.Receptionist;
using SWP_SportCenter.Service.Accounts;
using AthuService = SWP_SportCenter.Service.Athu;
using SWP_SportCenter.Service.Jwt;


Env.Load();

var aspnetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", aspnetCoreEnv);
var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("========================================");
Console.WriteLine("DEMO PASSWORD HASH:");
Console.WriteLine(Argon2Hasher.HashPassword("123456"));
Console.WriteLine("========================================");
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
    
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
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IReceptionistService, ReceptionistService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<AthuService.IService, AthuService.Service>();
builder.Services.AddScoped<IJwtService, JwtService>();


var allowedOrigins = builder.Configuration
                         .GetSection("Cors:AllowedOrigins")
                         .Get<string[]>()
                     ?? new[]
                     {
                         "http://localhost:5173",
                         "http://127.0.0.1:5173"
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



// Configure the HTTP request pipeline.
//test

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("FrontEnd"); 

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
