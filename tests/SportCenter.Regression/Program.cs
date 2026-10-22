using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using SWP_SportCenter.Controllers;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Repository;
using SWP_SportCenter.Repository.Entity;
using SWP_SportCenter.Repository.Enum;
using SWP_SportCenter.Service.Security;
using SWP_SportCenter.Service.Jwt;

// FIX: regression suite uses an isolated InMemory database; never contacts Render.
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
    ["JwtOptions:SecretKey"] = new string('t', 48), ["JwtOptions:Issuer"] = "tests", ["JwtOptions:Audience"] = "tests"
});
// One shared store for all scopes.
var store = new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();
builder.Services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase("regression", store));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentActor>();
// FIX B23: cố định 23:30 ngày 23/10 tại Việt Nam (16:30 UTC) để bắt lỗi lệch ngày/timezone.
var bookingNow = new DateTimeOffset(2026, 10, 23, 16, 30, 0, TimeSpan.Zero);
builder.Services.AddSingleton<TimeProvider>(new FixedClock(bookingNow));
builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; })
    .AddScheme<AuthenticationSchemeOptions, TestAuth>("Test", _ => { });
builder.Services.AddControllers(o => o.Filters.Add<SWP_SportCenter.Filters.PaginationFilter>())
    .AddApplicationPart(typeof(MemberController).Assembly);
builder.Services.AddScoped<SWP_SportCenter.Service.Register.IService, SWP_SportCenter.Service.Register.Service>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Member.IService, SWP_SportCenter.Service.Member.Service>();
builder.Services.AddScoped<SWP_SportCenter.Service.ClassBooking.IClassBookingService, SWP_SportCenter.Service.ClassBooking.ClassBookingService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Attendance.IAttendanceService, SWP_SportCenter.Service.Attendance.AttendanceService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Membership.IService, SWP_SportCenter.Service.Membership.Service>();
builder.Services.AddScoped<SWP_SportCenter.Service.Invoice.IInvoiceService, SWP_SportCenter.Service.Invoice.InvoiceService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Classes.IClassService, SWP_SportCenter.Service.Classes.ClassService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Dashboard.IDashboardService, SWP_SportCenter.Service.Dashboard.DashboardService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Notification.INotificationService, SWP_SportCenter.Service.Notification.NotificationService>();
builder.Services.AddScoped<SWP_SportCenter.Service.TrainingResult.IService, SWP_SportCenter.Service.TrainingResult.Service>();
builder.Services.AddScoped<SWP_SportCenter.Service.TrainingPlan.IService, SWP_SportCenter.Service.TrainingPlan.Service>();
builder.Services.AddScoped<SWP_SportCenter.Service.Coach.ICoachService, SWP_SportCenter.Service.Coach.CoachService>();
builder.Services.AddScoped<SWP_SportCenter.Service.MembershipPackage.IService, SWP_SportCenter.Service.MembershipPackage.Service>();
builder.Services.AddScoped<SWP_SportCenter.Service.SportCategory.ISportCategoryService, SWP_SportCenter.Service.SportCategory.SportCategoryService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Room.IRoomService, SWP_SportCenter.Service.Room.RoomService>();
builder.Services.AddScoped<SWP_SportCenter.Service.ClassSession.IClassSessionService, SWP_SportCenter.Service.ClassSession.ClassSessionService>();
var app = builder.Build();
app.UseMiddleware<SWP_SportCenter.Middleware.ApiExceptionMiddleware>();
app.UseAuthentication(); app.UseAuthorization(); app.MapControllers();
app.MapGet("/test/unexpected", (Func<IResult>)(() => throw new NullReferenceException("secret stack detail")));
await app.StartAsync();
var address = app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
    .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses.Single();
using var http = new HttpClient { BaseAddress = new Uri(address) };
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); passed++; }
void Login(Account a) => http.DefaultRequestHeaders.Authorization = new("Test", a.Id + "|" + a.Role);
async Task<JsonElement> Json(HttpResponseMessage r) => JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();
Account Account(AccountRole role, string name) => new() { Id = Guid.NewGuid(), Username = name, Email = name + "@test.local", Role = role, Status = AccountStatus.Active };
var memberAccount = Account(AccountRole.Member, "member"); var otherAccount = Account(AccountRole.Member, "other");
var coachAccount = Account(AccountRole.Coach, "coach"); var receptionistAccount = Account(AccountRole.Receptionist, "reception");
var manager = Account(AccountRole.CenterManager, "manager");
var member = new Member { Id = Guid.NewGuid(), Account = memberAccount, FullName = "Member", Phone = "0900000001", Dob = DateTime.UtcNow.AddYears(-20) };
var other = new Member { Id = Guid.NewGuid(), Account = otherAccount, FullName = "Other", Phone = "0900000002" };
var coach = new Coach { Id = Guid.NewGuid(), Account = coachAccount, FullName = "Coach", Phone = "0900000003" };
var receptionist = new Receptionist { Id = Guid.NewGuid(), Account = receptionistAccount, FullName = "Reception", Phone = "0900000004" };
var category = new SportCategory { Id = Guid.NewGuid(), CategoryName = "Fitness" };
var cls = new Class { Id = Guid.NewGuid(), ClassName = "Gym", Coach = coach, Category = category, MaxCapacity = 12, StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(20) };
// CODE CŨ: session chỉ có Date hôm nay, thiếu EndTime.
// FIX B23: fixture booking có buổi học tương lai rõ ràng.
var session = new ClassSession { Id = Guid.NewGuid(), Class = cls, Date = DateOnly.FromDateTime(bookingNow.ToOffset(TimeSpan.FromHours(7)).DateTime).AddDays(1),
    StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9) };
var package = new MembershipPackage { Id = Guid.NewGuid(), PackageName = "Monthly", DurationDays = 30, Price = 100 };
var membership = new Membership { Id = Guid.NewGuid(), Member = member, Package = package, StartDate = DateTime.UtcNow.AddDays(-20), EndDate = DateTime.UtcNow.AddDays(10), Status = MembershipStatus.Active };
var plan = new TrainingPlan { Id = Guid.NewGuid(), Coach = coach, Member = member, Class = cls, Title = "Strength plan", CreatedDate = DateTime.UtcNow };
var trainingResult = new TrainingResult { Id = Guid.NewGuid(), Coach = coach, Member = member, Plan = plan, CoachComment = "Good", PerformanceScore = 8, EvaluationDate = DateTime.UtcNow };
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.AddRange(member, other, coach, receptionist, manager, cls, session, membership, plan, trainingResult); await db.SaveChangesAsync();
}
Login(memberAccount);
var profileResponse = await http.GetAsync("/api/members/me");
if (!profileResponse.IsSuccessStatusCode) Console.WriteLine($"DEBUG {profileResponse.StatusCode}: {await profileResponse.Content.ReadAsStringAsync()}");
Check(profileResponse.IsSuccessStatusCode, "B2 Member profile uses NameIdentifier");
var profileBody = new { fullName = "Updated Member", phone = "0900000001", email = memberAccount.Email, dob = "2000-01-01", gender = "Other", trainingGoal = "Fitness" };
Check((await http.PutAsJsonAsync("/api/members/me", profileBody)).IsSuccessStatusCode, "B2 B17 update own profile");
var invalid = await http.PutAsJsonAsync("/api/members/me", new { fullName = "Missing required fields" });
Check(invalid.StatusCode == HttpStatusCode.BadRequest && (await Json(invalid)).TryGetProperty("errors", out _), "B17 field validation returns 400");
var booking = await http.PostAsJsonAsync("/api/class-bookings", new { memberId = other.Id, classId = cls.Id });
Check(booking.IsSuccessStatusCode && (await Json(booking)).GetProperty("memberId").GetGuid() == member.Id, "B7 forged MemberId ignored on booking");
Check((await http.GetAsync("/api/members/me/class-bookings")).IsSuccessStatusCode, "B4 own bookings alias");
Check((await http.GetAsync("/api/class-bookings/member/" + other.Id)).StatusCode == HttpStatusCode.Forbidden, "B7 other member history denied");
Login(receptionistAccount);
var otherBooking = await http.PostAsJsonAsync("/api/class-bookings", new { memberId = other.Id, classId = cls.Id });
Check(otherBooking.IsSuccessStatusCode, "B8 Receptionist books for member");
var otherBookingId = (await Json(otherBooking)).GetProperty("id").GetGuid();
Login(memberAccount);
Check((await http.DeleteAsync($"/api/class-bookings/{otherBookingId}/member/{other.Id}")).StatusCode == HttpStatusCode.NotFound, "B7 Member cannot cancel another member booking");
Login(manager);
var attendance = await http.PostAsJsonAsync("/api/attendances", new { sessionId = session.Id, memberId = member.Id, status = 1, recordedBy = otherAccount.Id });
Check(attendance.IsSuccessStatusCode && (await Json(attendance)).GetProperty("recordedBy").GetGuid() == manager.Id, "B15 Manager records attendance with token identity");
Login(memberAccount);
var history = await http.GetAsync("/api/members/me/attendances");
Check(history.IsSuccessStatusCode && (await Json(history))[0].GetProperty("classId").GetGuid() == cls.Id, "B5 own attendance includes class");
Check((await http.GetAsync("/api/attendances/member/" + other.Id)).StatusCode == HttpStatusCode.Forbidden, "B5 other attendance denied");
Check((await http.GetAsync("/api/members/me/invoices")).IsSuccessStatusCode && (await http.GetAsync("/api/invoices/members/me")).IsSuccessStatusCode, "B11 canonical and legacy invoice routes");
Login(manager);
Check((await http.PostAsync($"/api/memberships/{membership.Id}/cancel", null)).IsSuccessStatusCode, "B10 membership cancellation");
Login(receptionistAccount);
var renewed = await http.PostAsync($"/api/memberships/{membership.Id}/renew", null);
Check(renewed.IsSuccessStatusCode && (await Json(renewed)).GetProperty("membershipId").GetGuid() != membership.Id, "B20 renew creates new membership id");
using (var scope = app.Services.CreateScope()) {
 var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
 Check(await db.Invoices.CountAsync() == 1 && (await db.Memberships.FindAsync(membership.Id))!.Status == MembershipStatus.Cancelled, "B20 invoice created and previous period retained");
}
Login(manager);
using (var scope = app.Services.CreateScope()) {
 var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var invoiceId = (await db.Invoices.SingleAsync()).Id;
 Check((await http.GetAsync($"/api/invoices/{invoiceId}/export")).IsSuccessStatusCode, "B11 canonical export route");
}
Check((await http.PutAsJsonAsync($"/api/memberships/{membership.Id}", new { startDate = "2026-01-01", endDate = "2026-02-01", status = 3 })).IsSuccessStatusCode, "B20 PUT membership");
var noRecipients = await http.PostAsJsonAsync("/api/notifications", new { accountIds = new[] { Guid.NewGuid() }, title = "Test", message = "Test" });
Check(noRecipients.StatusCode == HttpStatusCode.BadRequest, "B16 empty valid recipient list returns 400");
var sent = await http.PostAsJsonAsync("/api/notifications", new { accountIds = new[] { memberAccount.Id, memberAccount.Id, Guid.NewGuid() }, title = "Test", message = "Test" });
Check(sent.IsSuccessStatusCode && (await Json(sent)).GetProperty("sentCount").GetInt32() == 1, "B16 recipients deduplicated and counted");
Check((await http.DeleteAsync($"/api/memberships/{membership.Id}")).IsSuccessStatusCode, "B20 soft DELETE membership");
Check((await http.GetAsync("/api/dashboard/revenue")).IsSuccessStatusCode, "B21 default revenue range");
Check((await http.GetAsync("/api/dashboard/revenue?from=2026-01-01")).StatusCode == HttpStatusCode.BadRequest, "B21 half range rejected");
Check((await http.GetAsync("/api/members?pageSize=501")).StatusCode == HttpStatusCode.BadRequest, "B19 page size capped");
http.DefaultRequestHeaders.Authorization = null;
foreach (var route in new[] { "/api/categories", "/api/classes", "/api/class-sessions", "/api/rooms", "/api/membership-packages", "/api/coaches" })
 Check((await http.GetAsync(route)).IsSuccessStatusCode, "B9 public " + route);
var publicCoaches = await Json(await http.GetAsync("/api/coaches"));
var publicClasses = await Json(await http.GetAsync("/api/classes"));
Check(publicClasses[0].GetProperty("currentEnrolled").GetInt32() == 2 && publicClasses[0].GetProperty("spotsLeft").GetInt32() == 10, "B14 class capacity visible to public");
Check((await Json(await http.GetAsync("/api/classes/" + cls.Id))).GetProperty("spotsLeft").GetInt32() == 10, "B14 detail capacity matches list");
Check(!publicCoaches[0].TryGetProperty("email", out _) && !publicCoaches[0].TryGetProperty("phone", out _), "B9 public coach contact redacted");
var badLogin = await http.PostAsJsonAsync("/api/auth/login", new { email = "missing", password = "bad" });
Check(badLogin.StatusCode == HttpStatusCode.Unauthorized && (await Json(badLogin)).GetProperty("status").GetInt32() == 401, "B6 login error is JSON 401");
var failure = await http.GetAsync("/test/unexpected"); var failureBody = await failure.Content.ReadAsStringAsync();
Check(failure.StatusCode == HttpStatusCode.InternalServerError && !failureBody.Contains("secret stack detail") && !failureBody.Contains("NullReferenceException"), "B6 unexpected errors hide stack trace");
var registered = await http.PostAsJsonAsync("/api/auth/register", new { username = "newmember", email = "new@test.local", fullName = "New Member", password = "12345678", phone = "0900000010", dob = "2001-02-03", gender = "Other", trainingGoal = "Strength" });
Check(registered.IsSuccessStatusCode, "B3 registration accepts complete form");
using (var scope = app.Services.CreateScope()) {
 var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
 var saved = await db.Members.Include(m => m.Account).SingleAsync(m => m.Account.Username == "newmember");
 Check(saved.Phone == "0900000010" && saved.Dob!.Value.Year == 2001 && saved.TrainingGoal == "Strength", "B3 B18 real profile values persisted");
}
Login(coachAccount);
// FIX B25: request có scope DbContext mới nên không thể dựa vào tracking/fixup từ dữ liệu seed.
var coachProfile = await http.GetAsync("/api/coaches/me");
Check(coachProfile.IsSuccessStatusCode && (await Json(coachProfile)).GetProperty("email").GetString() == coachAccount.Email,
    "B25 Coach me loads Account email");
Check((await http.GetAsync("/api/coaches/me/classes")).IsSuccessStatusCode, "B25 Coach me classes succeeds");
Check((await http.GetAsync("/api/coaches/me/members")).IsSuccessStatusCode, "B25 Coach me members succeeds");
http.DefaultRequestHeaders.Authorization = null;
var publicCoach = await http.GetAsync("/api/coaches/" + coach.Id);
Check(publicCoach.IsSuccessStatusCode && !(await Json(publicCoach)).TryGetProperty("email", out _),
    "B25 public Coach detail succeeds without leaking email");
Login(manager);
var staffCoach = await http.GetAsync("/api/coaches/" + coach.Id);
Check(staffCoach.IsSuccessStatusCode && (await Json(staffCoach)).GetProperty("email").GetString() == coachAccount.Email,
    "B25 staff Coach detail includes Account email");
Login(coachAccount);
var missingCoachAccount = Account(AccountRole.Coach, "no-profile");
Login(missingCoachAccount);
Check((await http.GetAsync("/api/coaches/me")).StatusCode == HttpStatusCode.NotFound, "B25 missing Coach profile returns 404");
Login(coachAccount);
var results = await http.GetAsync("/api/coaches/me/training-results?pageSize=500");
Check(results.IsSuccessStatusCode && (await Json(results)).GetProperty("data")[0].GetProperty("planTitle").GetString() == plan.Title, "B13 coach result listing includes plan title");
var plans = await http.GetAsync("/api/coaches/me/training-plans");
Check(plans.IsSuccessStatusCode && (await Json(plans)).GetProperty("data")[0].GetProperty("className").GetString() == cls.ClassName, "B13 training plan includes class name");
Check((await http.PostAsJsonAsync("/api/training-results", new { performanceScore = 11 })).StatusCode == HttpStatusCode.BadRequest, "B13 invalid score rejected");
// DTO validation covers missing real data before database access.
var input = new SWP_SportCenter.Service.Register.Request.RegisterRequest { FullName = "X", Username = "x", Email = "x@test.local", Password = "12345678" };
var errors = new List<ValidationResult>();
Check(!Validator.TryValidateObject(input, new ValidationContext(input), errors, true), "B3 missing phone/dob invalid");
using (var schema = new AppDbContextFactory().CreateDbContext(Array.Empty<string>())) {
    Check(!schema.Database.HasPendingModelChanges(), "Migration snapshot matches current model");
    var migrator = schema.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
    var sql = migrator.GenerateScript("20261010024745_fixdate", "20261022020000_MemberUnknownContact");
    Check(sql.Contains("ALTER TABLE account ADD full_name") && sql.Contains("ALTER TABLE member ALTER COLUMN phone DROP NOT NULL"), "PostgreSQL migration SQL generated without connection");
}
// FIX B23: kiểm tra qua HTTP cả Member và staff, không ghi database Render.
var vnNow = bookingNow.ToOffset(TimeSpan.FromHours(7));
var vnDate = DateOnly.FromDateTime(vnNow.DateTime);
var scenarios = new[] {
    ("No sessions", (DateOnly?)null, TimeSpan.Zero, false, ClassStatus.Ongoing, false),
    ("Past day", (DateOnly?)vnDate.AddDays(-1), TimeSpan.FromHours(23.9), false, ClassStatus.Ongoing, false),
    ("Ended today", (DateOnly?)vnDate, TimeSpan.FromHours(23), false, ClassStatus.Ongoing, false),
    ("Ends exactly now", (DateOnly?)vnDate, vnNow.TimeOfDay, false, ClassStatus.Ongoing, false),
    ("In progress", (DateOnly?)vnDate, TimeSpan.FromHours(23.75), false, ClassStatus.Ongoing, true),
    ("Tomorrow", (DateOnly?)vnDate.AddDays(1), TimeSpan.FromHours(1), false, ClassStatus.Ongoing, true),
    ("Deleted future session", (DateOnly?)vnDate.AddDays(1), TimeSpan.FromHours(1), true, ClassStatus.Ongoing, false),
    ("Completed with future session", (DateOnly?)vnDate.AddDays(1), TimeSpan.FromHours(1), false, ClassStatus.Completed, false),
    ("Cancelled with future session", (DateOnly?)vnDate.AddDays(1), TimeSpan.FromHours(1), false, ClassStatus.Cancelled, false)
};
foreach (var staff in new[] { false, true })
foreach (var scenario in scenarios) {
    Guid classId;
    using (var scope = app.Services.CreateScope()) {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = new Class { Id = Guid.NewGuid(), ClassName = scenario.Item1,
            CoachId = coach.Id, CategoryId = category.Id, MaxCapacity = 10, Status = scenario.Item5,
            StartDate = bookingNow.UtcDateTime.AddDays(-1), EndDate = bookingNow.UtcDateTime.AddDays(7) };
        db.Classes.Add(item);
        if (scenario.Item2.HasValue) db.ClassSessions.Add(new ClassSession {
            Id = Guid.NewGuid(), Class = item, Date = scenario.Item2.Value,
            StartTime = scenario.Item3 > TimeSpan.FromHours(1) ? scenario.Item3 - TimeSpan.FromHours(1) : TimeSpan.Zero,
            EndTime = scenario.Item3, IsDeleted = scenario.Item4
        });
        await db.SaveChangesAsync(); classId = item.Id;
    }
    Login(staff ? receptionistAccount : memberAccount);
    var response = await http.PostAsJsonAsync("/api/class-bookings", new { memberId = member.Id, classId });
    Check(scenario.Item6 ? response.IsSuccessStatusCode : response.StatusCode == HttpStatusCode.BadRequest,
        $"B23 {(staff ? "staff" : "member")} {scenario.Item1}");
    using var verify = app.Services.CreateScope();
    var count = await verify.ServiceProvider.GetRequiredService<AppDbContext>().ClassBookings.CountAsync(b => b.ClassId == classId);
    Check(count == (scenario.Item6 ? 1 : 0), $"B23 no unintended booking saved {staff} {scenario.Item1}");
}
Console.WriteLine($"RESULT {passed} checks passed");
await app.StopAsync();
await app.DisposeAsync();

sealed class TestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var value = Request.Headers.Authorization.ToString();
        if (!value.StartsWith("Test ")) return Task.FromResult(AuthenticateResult.NoResult());
        var parts = value[5..].Split('|');
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
            new Claim(ClaimTypes.NameIdentifier, parts[0]), new Claim(ClaimTypes.Role, parts[1]) }, "Test"));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, "Test")));
    }
}

sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
