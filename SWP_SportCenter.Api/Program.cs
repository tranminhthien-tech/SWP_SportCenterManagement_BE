using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository;


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

var builder = WebApplication.CreateBuilder(args);

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

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();