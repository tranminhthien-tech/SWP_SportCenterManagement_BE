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
// CODE CŨ: DbContext test không có audit interceptor.
// FIX B33: kiểm tra cùng interceptor với server thật.
builder.Services.AddScoped<SWP_SportCenter.Infrastructure.ServerAuditInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, o) => o.UseInMemoryDatabase("regression", store)
    .AddInterceptors(sp.GetRequiredService<SWP_SportCenter.Infrastructure.ServerAuditInterceptor>()));
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
// FIX 23/10: services bổ sung cho kiểm thử mới; không bật background worker trong test.
builder.Services.AddScoped<SWP_SportCenter.Service.Support.SupportRequestService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Notification.MembershipExpiryNotifier>();
builder.Services.AddScoped<SWP_SportCenter.Service.AuditLog.IAuditLogService, SWP_SportCenter.Service.AuditLog.AuditLogService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Accounts.IAccountService, SWP_SportCenter.Service.Accounts.AccountService>();
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
// CODE CŨ: Manager ghi điểm danh thành công theo B15 cũ.
// FIX 23/10: thay kỳ vọng theo nghiệp vụ mới, chỉ Coach sở hữu lớp được ghi.
var attendanceBody = new { sessionId = session.Id, memberId = member.Id, status = 1, recordedBy = otherAccount.Id };
Check((await http.PostAsJsonAsync("/api/attendances", attendanceBody)).StatusCode == HttpStatusCode.Forbidden, "Attendance Manager cannot record");
Login(receptionistAccount);
Check((await http.PostAsJsonAsync("/api/attendances", attendanceBody)).StatusCode == HttpStatusCode.Forbidden, "Attendance Receptionist cannot record");
Login(coachAccount);
var attendance = await http.PostAsJsonAsync("/api/attendances", attendanceBody);
Check(attendance.IsSuccessStatusCode && (await Json(attendance)).GetProperty("recordedBy").GetGuid() == coachAccount.Id, "Attendance owner Coach records with token identity");
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
// FIX 23/10: regression B24 B26-B34 và quyền điểm danh, độc lập dữ liệu production.
var attendanceId = (await Json(attendance)).GetProperty("id").GetGuid();
foreach (var reader in new[] { manager, receptionistAccount }) {
    Login(reader);
    Check((await http.GetAsync($"/api/attendances/session/{session.Id}")).IsSuccessStatusCode, "Attendance staff may read " + reader.Role);
    Check((await http.PutAsJsonAsync($"/api/attendances/{attendanceId}", new { status = 2 })).StatusCode == HttpStatusCode.Forbidden,
        "Attendance staff cannot update " + reader.Role);
}
var foreignCoachAccount = Account(AccountRole.Coach, "foreign-coach");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Coaches.Add(new Coach { Id = Guid.NewGuid(), Account = foreignCoachAccount, FullName = "Foreign Coach", Phone = "0900000059" });
    await db.SaveChangesAsync();
}
Login(foreignCoachAccount);
Check((await http.PostAsJsonAsync("/api/attendances", new { sessionId = session.Id, memberId = other.Id, status = 1 })).StatusCode == HttpStatusCode.Forbidden,
    "Attendance foreign Coach cannot create");
Check((await http.PutAsJsonAsync($"/api/attendances/{attendanceId}", new { status = 2 })).StatusCode == HttpStatusCode.Forbidden,
    "Attendance foreign Coach cannot update");
Login(coachAccount);
Check((await http.PutAsJsonAsync($"/api/attendances/{attendanceId}", new { status = 2 })).IsSuccessStatusCode, "Attendance owner Coach can update");
Login(receptionistAccount);
Check((await http.GetAsync($"/api/classes/{cls.Id}/members")).IsSuccessStatusCode, "B28 Receptionist reads roster directly");
var memberMetadata = await Json(await http.GetAsync($"/api/members/{member.Id}"));
Check(memberMetadata.TryGetProperty("createdAt", out _) && memberMetadata.GetProperty("accountStatus").GetInt32() == 1, "B24 member includes account metadata");
Check((await http.PutAsJsonAsync($"/api/members/{member.Id}", new { accountId = memberAccount.Id, fullName = "Counter update", phone = "0900000001", email = memberAccount.Email, dob = "2000-01-01", gender = "Other", trainingGoal = "Strength" })).IsSuccessStatusCode,
    "B24 Receptionist edits member at counter");
// Member phục hồi giữ ID và không tạo bản ghi thứ hai.
var restoredAccount = Account(AccountRole.Member, "restore-member");
var restoredId = Guid.NewGuid();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Members.Add(new Member { Id = restoredId, Account = restoredAccount, FullName = "Deleted", Phone = "0900000058", IsDeleted = true });
    await db.SaveChangesAsync();
}
var restoreBody = new { accountId = restoredAccount.Id, fullName = "Restored", phone = "0900000058", email = restoredAccount.Email, dob = "2000-01-01", trainingGoal = "Mobility" };
var restored = await http.PostAsJsonAsync("/api/members", restoreBody);
Check(restored.IsSuccessStatusCode && (await Json(restored)).GetProperty("memberId").GetGuid() == restoredId, "B27 restores soft-deleted member with same ID");
Check((await http.PostAsJsonAsync("/api/members", restoreBody)).StatusCode == HttpStatusCode.BadRequest, "B27 active profile still rejects duplicate");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Check(await db.Members.CountAsync(x => x.AccountId == restoredAccount.Id) == 1 && !(await db.Members.FindAsync(restoredId))!.IsDeleted, "B27 no duplicate profile stored");
}
Login(coachAccount);
var coachMembers = await Json(await http.GetAsync("/api/coaches/me/members"));
Check(coachMembers.EnumerateArray().Any(x => x.GetProperty("memberId").GetGuid() == member.Id &&
    x.GetProperty("trainingGoal").GetString() == "Strength" && x.TryGetProperty("gender", out _) && x.TryGetProperty("dob", out _)), "B30 coach members includes goals gender dob");
var sendOwn = await http.PostAsJsonAsync("/api/notifications", new { accountIds = new[] { memberAccount.Id }, title = "Homework", message = "Stretch" });
Check(sendOwn.IsSuccessStatusCode, "B29 Coach sends own student homework");
int notificationsBefore;
using (var scope = app.Services.CreateScope()) notificationsBefore = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Notifications.CountAsync();
Check((await http.PostAsJsonAsync("/api/notifications", new { accountIds = new[] { memberAccount.Id, manager.Id }, title = "Forbidden", message = "No" })).StatusCode == HttpStatusCode.Forbidden,
    "B29 mixed authorized unauthorized recipients rejected");
using (var scope = app.Services.CreateScope()) Check(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Notifications.CountAsync() == notificationsBefore, "B29 rejection sends nothing");
Login(foreignCoachAccount);
Check((await http.PostAsJsonAsync("/api/notifications", new { accountIds = new[] { memberAccount.Id }, title = "No", message = "No" })).StatusCode == HttpStatusCode.Forbidden, "B29 other Coach cannot contact student");
Login(receptionistAccount);
var duplicateBooking = await http.PostAsJsonAsync("/api/class-bookings", new { classId = cls.Id, memberId = member.Id });
Check(duplicateBooking.StatusCode == HttpStatusCode.Conflict && (await Json(duplicateBooking)).GetProperty("message").GetString() == "Học viên này đã đăng ký lớp học này rồi.", "B34 duplicate is neutral JSON 409");
// B31: token ownership và quyền xử lý.
Login(memberAccount);
var support = await http.PostAsJsonAsync("/api/support-requests", new { content = "Need help at the counter", memberId = other.Id });
Check(support.StatusCode == HttpStatusCode.Created && (await Json(support)).GetProperty("memberId").GetGuid() == member.Id, "B31 support creator from token");
var supportId = (await Json(support)).GetProperty("id").GetGuid();
Check((await http.PutAsJsonAsync($"/api/support-requests/{supportId}", new { status = 3 })).StatusCode == HttpStatusCode.Forbidden, "B31 Member cannot handle requests");
Check((await http.PostAsJsonAsync("/api/support-requests", new { content = "   " })).StatusCode == HttpStatusCode.BadRequest, "B31 blank content rejected");
Login(otherAccount);
Check((await Json(await http.GetAsync($"/api/support-requests?memberId={member.Id}"))).GetProperty("totalItems").GetInt32() == 0, "B31 Member cannot list another member requests");
Login(receptionistAccount);
Check((await http.PutAsJsonAsync($"/api/support-requests/{supportId}", new { status = 2, resolution = "Checking", handledByAccountId = manager.Id })).IsSuccessStatusCode, "B31 Receptionist handles request");
var supportPage = await Json(await http.GetAsync("/api/support-requests?status=2"));
Check(supportPage.GetProperty("data")[0].GetProperty("handledByAccountId").GetGuid() == receptionistAccount.Id, "B31 handler comes from token");
Check((await http.PutAsJsonAsync($"/api/support-requests/{supportId}", new { status = 99 })).StatusCode == HttpStatusCode.BadRequest, "B31 invalid status rejected");
Login(coachAccount);
Check((await http.GetAsync("/api/support-requests")).StatusCode == HttpStatusCode.Forbidden, "B31 Coach cannot read support requests");
// B26: chốt giữ hai bước, không tạo hồ sơ staff ngầm.
Login(manager);
foreach (var role in new[] { AccountRole.Coach, AccountRole.Receptionist, AccountRole.CenterManager }) {
    var createdAccount = await http.PostAsJsonAsync("/api/accounts", new { username = "two-step-" + role, email = role + "@workflow.local", password = "12345678", role = (int)role, fullName = "Not yet a profile", phone = "0900000060" });
    Check(createdAccount.IsSuccessStatusCode && !(await Json(createdAccount)).GetProperty("hasProfile").GetBoolean(), "B26 new staff account has no profile " + role);
    var newStaffId = (await Json(createdAccount)).GetProperty("accountId").GetGuid();
    using (var verify = app.Services.CreateScope()) {
        var db = verify.ServiceProvider.GetRequiredService<AppDbContext>();
        Check(!await db.Coaches.AnyAsync(x => x.AccountId == newStaffId) &&
            !await db.Receptionists.AnyAsync(x => x.AccountId == newStaffId) &&
            !await db.CenterManagers.AnyAsync(x => x.AccountId == newStaffId), "B26 no implicit staff profile stored " + role);
    }
    if (role == AccountRole.Coach) {
        var explicitProfile = await http.PostAsJsonAsync("/api/coaches", new { accountId = newStaffId, fullName = "Explicit Coach", phone = "0900000060", email = role + "@workflow.local", specialization = "Gym", experienceYears = 2 });
        Check(explicitProfile.IsSuccessStatusCode, "B26 explicit second step creates Coach profile");
        var accountList = await Json(await http.GetAsync("/api/accounts?searchTerm=two-step-Coach&hasProfile=true"));
        Check(accountList.GetProperty("data").EnumerateArray().Any(x => x.GetProperty("accountId").GetGuid() == newStaffId && x.GetProperty("hasProfile").GetBoolean()), "B26 completed profile visible to filtered account list");
    }
}
// B32: đổi lịch và xóa lịch tạo thông báo cùng lần SaveChanges.
var room = new Room { Id = Guid.NewGuid(), RoomName = "Notify room", Capacity = 30, Status = RoomStatus.Available };
var notifySession = new ClassSession { Id = Guid.NewGuid(), ClassId = cls.Id, Room = room, Date = vnDate.AddDays(2), StartTime = TimeSpan.FromHours(10), EndTime = TimeSpan.FromHours(11) };
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var classRow = await db.Classes.FindAsync(cls.Id); classRow!.StartDate = bookingNow.UtcDateTime.AddDays(-1); classRow.EndDate = bookingNow.UtcDateTime.AddDays(20);
    db.ClassSessions.Add(notifySession); await db.SaveChangesAsync();
    notificationsBefore = await db.Notifications.CountAsync();
}
var sessionBody = new { classId = cls.Id, roomId = room.Id, date = vnDate.AddDays(2).ToString("yyyy-MM-dd"), startTime = "12:00:00", endTime = "13:00:00" };
Check((await http.PutAsJsonAsync($"/api/class-sessions/{notifySession.Id}", sessionBody)).IsSuccessStatusCode, "B32 reschedule succeeds");
using (var scope = app.Services.CreateScope()) Check(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Notifications.CountAsync() == notificationsBefore + 2, "B32 enrolled students notified of schedule change");
Check((await http.PutAsJsonAsync($"/api/class-sessions/{notifySession.Id}", sessionBody)).IsSuccessStatusCode, "B32 unchanged schedule succeeds");
using (var scope = app.Services.CreateScope()) Check(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Notifications.CountAsync() == notificationsBefore + 2, "B32 unchanged schedule sends no duplicate");
Check((await http.DeleteAsync($"/api/class-sessions/{notifySession.Id}")).IsSuccessStatusCode, "B32 delete session succeeds");
using (var scope = app.Services.CreateScope()) Check(await scope.ServiceProvider.GetRequiredService<AppDbContext>().Notifications.CountAsync() == notificationsBefore + 4, "B32 students notified when session deleted");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Memberships.Add(new Membership { Id = Guid.NewGuid(), MemberId = member.Id, PackageId = package.Id,
        StartDate = bookingNow.UtcDateTime.AddDays(-20), EndDate = DateTime.SpecifyKind(vnDate.AddDays(3).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc), Status = MembershipStatus.Active });
    await db.SaveChangesAsync();
    var notifier = scope.ServiceProvider.GetRequiredService<SWP_SportCenter.Service.Notification.MembershipExpiryNotifier>();
    Check(await notifier.RunAsync() == 1, "B32 expiry reminder generated");
    Check(await notifier.RunAsync() == 0, "B32 repeated job does not duplicate reminder");
    var reminder = await db.Notifications.SingleAsync(n => n.DeduplicationKey != null);
    reminder.IsDeleted = true; await db.SaveChangesAsync();
    Check(await notifier.RunAsync() == 0, "B32 deleting reminder does not trigger resend");
    // Trường hợp ngoài cửa sổ và gói không hoạt động không được nhắc.
    foreach (var item in new[] { (days: -1, status: MembershipStatus.Active), (days: 8, status: MembershipStatus.Active), (days: 2, status: MembershipStatus.Cancelled) })
        db.Memberships.Add(new Membership { Id = Guid.NewGuid(), MemberId = member.Id, PackageId = package.Id,
            StartDate = bookingNow.UtcDateTime.AddDays(-20), EndDate = DateTime.SpecifyKind(vnDate.AddDays(item.days).ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc), Status = item.status });
    await db.SaveChangesAsync();
    Check(await notifier.RunAsync() == 0, "B32 excludes expired distant and cancelled memberships");
}
// B33: thao tác của cả ba staff tự ghi log, POST không thể giả AccountId.
var auditPage = await Json(await http.GetAsync("/api/audit-logs?pageSize=500"));
Check(auditPage.GetProperty("data").GetArrayLength() > 0 && auditPage.GetProperty("totalItems").GetInt32() > 0, "B33 automatic audit and paginated response");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    foreach (var staff in new[] { coachAccount, receptionistAccount, manager })
        Check(await db.AuditLogs.AnyAsync(x => x.AccountId == staff.Id && x.Description.Contains("server")), "B33 automatic audit for " + staff.Role);
}
Login(receptionistAccount);
var manualAudit = await http.PostAsJsonAsync("/api/audit-logs", new { accountId = manager.Id, actionType = "ManualNote", description = "Counter action" });
Check(manualAudit.IsSuccessStatusCode && (await Json(manualAudit)).GetProperty("accountId").GetGuid() == receptionistAccount.Id, "B33 POST audit ignores forged actor");
Check((await http.GetAsync("/api/audit-logs")).StatusCode == HttpStatusCode.Forbidden, "B33 only Manager reads audits");
Login(manager);
var filteredAudits = await Json(await http.GetAsync($"/api/audit-logs?accountId={receptionistAccount.Id}&actionType=ManualNote&pageSize=1"));
Check(filteredAudits.GetProperty("totalItems").GetInt32() == 1 && filteredAudits.GetProperty("data")[0].GetProperty("accountId").GetGuid() == receptionistAccount.Id, "B33 audit filters applied before pagination");
Check((await http.GetAsync("/api/audit-logs?from=2026-10-25T00:00:00Z&to=2026-10-23T00:00:00Z")).StatusCode == HttpStatusCode.BadRequest, "B33 inverted audit time range rejected");
using (var schema = new AppDbContextFactory().CreateDbContext(Array.Empty<string>())) {
    var sql = schema.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>()
        .GenerateScript("20261022020000_MemberUnknownContact", "20261023060213_StaffWorkflowSupportNotifications");
    Check(sql.Contains("support_request") && sql.Contains("DeduplicationKey"), "B31 B32 PostgreSQL schema SQL generated");
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
