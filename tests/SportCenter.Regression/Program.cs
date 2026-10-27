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
var workflowSaves = new WorkflowSaveCounter(); // FIX B49: đếm lần lưu thật trong request batch.
// CODE CŨ: DbContext test không có audit interceptor.
// FIX B33: kiểm tra cùng interceptor với server thật.
builder.Services.AddScoped<SWP_SportCenter.Infrastructure.ServerAuditInterceptor>();
builder.Services.AddDbContext<AppDbContext>((sp, o) => o.UseInMemoryDatabase("regression", store)
    .AddInterceptors(sp.GetRequiredService<SWP_SportCenter.Infrastructure.ServerAuditInterceptor>(), workflowSaves));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentActor>();
// FIX B23: cố định 23:30 ngày 23/10 tại Việt Nam (16:30 UTC) để bắt lỗi lệch ngày/timezone.
var bookingNow = new DateTimeOffset(2026, 10, 23, 16, 30, 0, TimeSpan.Zero);
builder.Services.AddSingleton<TimeProvider>(new FixedClock(bookingNow));
builder.Services.AddJwtServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerServices(); // FIX B22: test tránh xung đột Swagger của route JSON/FormData.
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
builder.Services.AddScoped<SWP_SportCenter.Service.Payment.IPaymentService, SWP_SportCenter.Service.Payment.PaymentService>();
// FIX B22 B39: test upload giả lập không dùng secret/cloud thật, expiration dùng clock cố định.
builder.Services.AddScoped<SWP_SportCenter.Service.MediaService.IService, TestMedia>();
builder.Services.AddScoped<SWP_SportCenter.Service.Receptionist.IReceptionistService, SWP_SportCenter.Service.Receptionist.ReceptionistService>();
builder.Services.AddScoped<SWP_SportCenter.Service.CenterManager.ICenterManagerService, SWP_SportCenter.Service.CenterManager.CenterManagerService>();
builder.Services.AddScoped<SWP_SportCenter.Service.Payment.PaymentExpirationService>();
builder.Services.AddScoped<SWP_SportCenter.Service.CheckIns.CheckInService>(); // FIX B47
var app = builder.Build();
app.UseMiddleware<SWP_SportCenter.Middleware.ApiExceptionMiddleware>();
app.UseSwaggerAPI(); // FIX B22
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
// CODE CŨ: fixture thiếu Status (0 không hợp lệ).
var package = new MembershipPackage { Id = Guid.NewGuid(), PackageName = "Monthly", DurationDays = 30, Price = 100, Status = MembershipStatus.Active }; // FIX B39
var membership = new Membership { Id = Guid.NewGuid(), Member = member, Package = package, StartDate = DateTime.UtcNow.AddDays(-20), EndDate = DateTime.UtcNow.AddDays(10), Status = MembershipStatus.Active };
var plan = new TrainingPlan { Id = Guid.NewGuid(), Coach = coach, Member = member, Class = cls, Title = "Strength plan", CreatedDate = DateTime.UtcNow };
var trainingResult = new TrainingResult { Id = Guid.NewGuid(), Coach = coach, Member = member, Plan = plan, CoachComment = "Good", PerformanceScore = 8, EvaluationDate = DateTime.UtcNow };
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.AddRange(member, other, coach, receptionist, manager, cls, session, membership, plan, trainingResult); await db.SaveChangesAsync();
    // FIX B45: fixture booking độc lập với thẻ đang được các test cancel/renew/delete.
    db.Memberships.AddRange(new Membership { Id = Guid.NewGuid(), MemberId = member.Id, PackageId = package.Id,
        StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), Status = MembershipStatus.Active },
        new Membership { Id = Guid.NewGuid(), MemberId = other.Id, PackageId = package.Id,
        StartDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), EndDate = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc), Status = MembershipStatus.Active });
    await db.SaveChangesAsync();
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
// FIX B35: log Added ghi đúng ID thật của bản ghi, không dùng ID tạm; ghi nhận giá trị cũ -> mới (changes).
Login(manager);
var createRoomResp = await http.PostAsJsonAsync("/api/rooms", new { roomName = "AuditRoomB35", capacity = 15, status = 1 });
Check(createRoomResp.IsSuccessStatusCode, "B35 create room succeeds");
var createdRoomId = (await Json(createRoomResp)).GetProperty("id").GetGuid();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roomAddLog = await db.AuditLogs.OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync(x => x.ActionType == "Added:Room" && x.Description.Contains(createdRoomId.ToString()));
    Check(roomAddLog != null && roomAddLog.Description.Contains("AuditRoomB35") && roomAddLog.Description.Contains("\"changes\""), "B35 Added:Room audit log matches real entityId and initial values");
}
var updateRoomResp = await http.PutAsJsonAsync($"/api/rooms/{createdRoomId}", new { roomName = "AuditRoomB35_Renamed", capacity = 20, status = 1 });
Check(updateRoomResp.IsSuccessStatusCode, "B35 update room succeeds");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var roomModLog = await db.AuditLogs.OrderByDescending(x => x.CreatedAt)
        .FirstOrDefaultAsync(x => x.ActionType == "Modified:Room" && x.Description.Contains(createdRoomId.ToString()));
    Check(roomModLog != null && roomModLog.Description.Contains("AuditRoomB35") && roomModLog.Description.Contains("AuditRoomB35_Renamed"), "B35 Modified:Room audit log tracks old and new values");
    // Kiểm tra không lộ trường nhạy cảm trong bất kỳ audit log nào.
    var sensitiveLogs = await db.AuditLogs.Where(x => x.Description.Contains("\"Password\"") || x.Description.Contains("\"Token\"")).ToListAsync();
    Check(sensitiveLogs.Count == 0, "B35 sensitive fields excluded from audit logs");
}
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

// FIX B36: Doi mat khau hoc vien / nhan vien
Login(memberAccount);
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var acc = await db.Accounts.FindAsync(memberAccount.Id);
    acc!.Password = SWP_SportCenter.Service.Until.Argon2Hasher.HashPassword("OldPass1234");
    await db.SaveChangesAsync();
}
var badOldPass = await http.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "WrongPassword", newPassword = "NewPass1234" });
Check(badOldPass.StatusCode == HttpStatusCode.BadRequest && (await Json(badOldPass)).GetProperty("message").GetString() == "Mật khẩu hiện tại không đúng.", "B36 change password rejects wrong current password");
var shortNewPass = await http.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "OldPass1234", newPassword = "short" });
Check(shortNewPass.StatusCode == HttpStatusCode.BadRequest, "B36 change password rejects short new password");
var samePass = await http.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "OldPass1234", newPassword = "OldPass1234" });
Check(samePass.StatusCode == HttpStatusCode.BadRequest, "B36 change password rejects same new password");
var successChangePass = await http.PostAsJsonAsync("/api/auth/change-password", new { currentPassword = "OldPass1234", newPassword = "BrandNewPassword123" });
Check(successChangePass.IsSuccessStatusCode, "B36 change password succeeds");

// FIX B37: Thong tin cong khai HLV bo sung dob, gender
http.DefaultRequestHeaders.Authorization = null;
var publicCoachList = await Json(await http.GetAsync("/api/coaches"));
Check(publicCoachList[0].TryGetProperty("dob", out _) && publicCoachList[0].TryGetProperty("gender", out _), "B37 public coach list includes dob and gender");
var publicCoachDetail = await Json(await http.GetAsync("/api/coaches/" + coach.Id));
Check(publicCoachDetail.TryGetProperty("dob", out _) && publicCoachDetail.TryGetProperty("gender", out _), "B37 public coach detail includes dob and gender");
Login(manager);
var staffCoachDetail = await Json(await http.GetAsync("/api/coaches/" + coach.Id));
Check(staffCoachDetail.TryGetProperty("phone", out _) && staffCoachDetail.TryGetProperty("email", out _) && staffCoachDetail.TryGetProperty("dob", out _) && staffCoachDetail.TryGetProperty("gender", out _), "B37 staff coach detail includes all fields");

// FIX B38: Dang ky va thanh toan goi tap truc tuyen
Login(memberAccount);
var checkoutResp = await http.PostAsJsonAsync("/api/payments/checkout", new {
    membershipPackageId = package.Id,
    startDate = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    paymentMethod = 2,
    returnUrl = "http://localhost:3000/payment/result"
});
Check(checkoutResp.IsSuccessStatusCode, "B38 checkout package succeeds");
var checkoutData = await Json(checkoutResp);
var paymentId = checkoutData.GetProperty("paymentId").GetGuid();
var checkoutMembershipId = checkoutData.GetProperty("membershipId").GetGuid();
// CODE CŨ: kỳ vọng URL mock tự xác nhận không token.
Check(checkoutData.GetProperty("status").GetInt32() == 1 && checkoutData.GetProperty("paymentUrl").ValueKind == JsonValueKind.Null, "B39 checkout Pending without unsafe mock URL");

var paymentDetail = await Json(await http.GetAsync($"/api/payments/{paymentId}"));
Check(paymentDetail.GetProperty("status").GetInt32() == 1 && paymentDetail.GetProperty("amount").GetDecimal() == package.Price && paymentDetail.GetProperty("packageName").GetString() == package.PackageName, "B38 get payment detail returns Pending status");

Login(otherAccount);
Check((await http.GetAsync($"/api/payments/{paymentId}")).StatusCode == HttpStatusCode.Forbidden, "B38 other member cannot view payment");

// FIX B39: người khác biết ID cũng không tự đánh dấu Paid được.
Check((await http.PostAsync($"/api/payments/{paymentId}/confirm", null)).StatusCode == HttpStatusCode.Forbidden, "B39 Member cannot confirm");
http.DefaultRequestHeaders.Authorization = null;
Check((await http.PostAsync($"/api/payments/{paymentId}/confirm", null)).StatusCode == HttpStatusCode.Unauthorized, "B39 anonymous confirm denied");
Check((await http.GetAsync($"/api/payments/mock-gateway?paymentId={paymentId}&returnUrl=https://invalid.local")).StatusCode == HttpStatusCode.Unauthorized, "B39 anonymous mock denied");
Login(receptionistAccount);
var confirmResp = await http.PostAsync($"/api/payments/{paymentId}/confirm", null);
Check(confirmResp.IsSuccessStatusCode, "B38 payment confirmation succeeds");
Login(memberAccount);
var paidPaymentDetail = await Json(await http.GetAsync($"/api/payments/{paymentId}"));
Check(paidPaymentDetail.GetProperty("status").GetInt32() == 2 && paidPaymentDetail.GetProperty("paidAt").GetString() != null, "B38 payment detail updated to Paid with paidAt");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var activatedMembership = await db.Memberships.FindAsync(checkoutMembershipId);
    Check(activatedMembership!.Status == MembershipStatus.Active, "B38 membership activated upon payment confirmation");
}
// FIX B39: checkout không gán người thu/ngày trả trước thanh toán.
Login(memberAccount);
async Task<Guid> CheckoutTest() {
    var r = await http.PostAsJsonAsync("/api/payments/checkout", new { membershipPackageId = package.Id, startDate = "2026-10-25", paymentMethod = 2 });
    Check(r.IsSuccessStatusCode, "B39 create pending checkout fixture");
    return (await Json(r)).GetProperty("paymentId").GetGuid();
}
var pendingId = await CheckoutTest();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var row = await db.Invoices.FindAsync(pendingId);
    Check(row!.ReceptionistId == null && row.PaymentDate == null && row.PaymentExpiresAt.HasValue, "B39 Pending has no collector/paid date and has TTL");
}
Login(manager);
Check((await http.GetAsync($"/api/payments/mock-gateway?paymentId={pendingId}&returnUrl=https://invalid.local")).StatusCode == HttpStatusCode.Gone, "B39 authorized GET mock cannot mutate");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var row = await db.Invoices.FindAsync(pendingId); row!.Status = InvoiceStatus.Cancelled; await db.SaveChangesAsync();
}
Check((await http.PostAsync($"/api/payments/{pendingId}/confirm", null)).StatusCode == HttpStatusCode.Conflict, "B39 cancelled invoice cannot be paid");
Login(memberAccount); var expiredId = await CheckoutTest();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    (await db.Invoices.FindAsync(expiredId))!.PaymentExpiresAt = bookingNow.AddMinutes(-1); await db.SaveChangesAsync();
}
Login(manager);
Check((await http.PostAsync($"/api/payments/{expiredId}/confirm", null)).StatusCode == HttpStatusCode.Conflict, "B39 expired invoice cannot be paid");
using (var scope = app.Services.CreateScope()) {
    Check(await scope.ServiceProvider.GetRequiredService<SWP_SportCenter.Service.Payment.PaymentExpirationService>().ExpireAsync() == 1, "B39 abandoned checkout expires");
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var row = await db.Invoices.Include(i => i.Membership).SingleAsync(i => i.Id == expiredId);
    Check(row.Status == InvoiceStatus.Cancelled && row.Membership.Status == MembershipStatus.Cancelled && row.PaymentDate == null, "B39 expiration cancels inactive membership without fake payment");
}
var paidAtBefore = paidPaymentDetail.GetProperty("paidAt").GetString();
Check((await http.PostAsync($"/api/payments/{paymentId}/confirm", null)).IsSuccessStatusCode, "B39 paid confirmation idempotent");
Check((await Json(await http.GetAsync($"/api/payments/{paymentId}"))).GetProperty("paidAt").GetString() == paidAtBefore, "B39 retry preserves original paid date");

// FIX B40: RoomService.Update(entity) đánh dấu mọi trường nhưng audit chỉ giữ diff thật.
var auditRoom = await Json(await http.PostAsJsonAsync("/api/rooms", new { roomName = "DiffRoom", capacity = 10, status = 1 }));
var auditRoomId = auditRoom.GetProperty("id").GetGuid();
Check((await http.PutAsJsonAsync($"/api/rooms/{auditRoomId}", new { roomName = "DiffRoom", capacity = 11, status = 1 })).IsSuccessStatusCode, "B40 update one room field");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logs = await db.AuditLogs.Where(a => a.ActionType == "Modified:Room").ToListAsync();
    var payload = logs.Select(a => JsonDocument.Parse(a.Description).RootElement.Clone()).Single(x => x.GetProperty("entityId").GetGuid() == auditRoomId);
    var fields = payload.GetProperty("changedFields").EnumerateArray().Select(x => x.GetString()).ToArray();
    Check(fields.Contains("Capacity") && !fields.Contains("RoomName") && !fields.Contains("Status") && !fields.Contains("CreatedAt"), "B40 excludes fields whose original equals current");
}
// FIX B41: định dạng mới tự mô tả, không mất khả năng kiểm tra hash legacy.
var hashNew = SWP_SportCenter.Service.Until.Argon2Hasher.HashPassword("HashTest123");
Check(hashNew.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$") && SWP_SportCenter.Service.Until.Argon2Hasher.VerifyHash("HashTest123", hashNew), "B41 new self-describing hash verifies");
Check(!SWP_SportCenter.Service.Until.Argon2Hasher.VerifyHash("Wrong", hashNew) && !SWP_SportCenter.Service.Until.Argon2Hasher.VerifyHash("Wrong", "$argon2id$v=19$m=999999999,t=2,p=1$a$b"), "B41 wrong/malformed hashes fail safely");
string legacy;
var legacySalt = System.Security.Cryptography.RandomNumberGenerator.GetBytes(16);
using (var argon = new Konscious.Security.Cryptography.Argon2id(System.Text.Encoding.UTF8.GetBytes("LegacyTest123")) { Salt = legacySalt, MemorySize = 131072, Iterations = 4, DegreeOfParallelism = 8 })
    legacy = Convert.ToBase64String(legacySalt) + ":" + Convert.ToBase64String(argon.GetBytes(16));
Check(SWP_SportCenter.Service.Until.Argon2Hasher.VerifyHash("LegacyTest123", legacy), "B41 legacy 128MiB hash verifies");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    (await db.Accounts.FindAsync(otherAccount.Id))!.Password = legacy; await db.SaveChangesAsync();
}
http.DefaultRequestHeaders.Authorization = null;
Check((await http.PostAsJsonAsync("/api/auth/login", new { email = otherAccount.Email, password = "LegacyTest123" })).IsSuccessStatusCode, "B41 legacy login succeeds");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Check(!SWP_SportCenter.Service.Until.Argon2Hasher.NeedsRehash((await db.Accounts.FindAsync(otherAccount.Id))!.Password), "B41 successful legacy login upgrades hash");
}

// FIX B22: dùng ảnh PNG 1 pixel, fake cloud có kiểm tra định dạng nhưng không ra mạng.
var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+j2i8AAAAASUVORK5CYII=");
MultipartFormDataContent ImageForm(Dictionary<string,string>? values = null, byte[]? data = null, string filename = "avatar.png") {
    var body = new MultipartFormDataContent();
    if (values != null) foreach (var pair in values) body.Add(new StringContent(pair.Value), pair.Key);
    body.Add(new ByteArrayContent(data ?? png), values == null ? "file" : "avatar", filename);
    return body;
}
Check((await http.PostAsync("/api/media/images", ImageForm())).StatusCode == HttpStatusCode.Unauthorized, "B22 anonymous upload denied");
Login(memberAccount);
Check((await http.PostAsync("/api/media/images", ImageForm(data: new byte[] {1,2,3}))).StatusCode == HttpStatusCode.BadRequest, "B22 fake image signature rejected");
var uploaded = await http.PostAsync("/api/media/images", ImageForm());
Check(uploaded.IsSuccessStatusCode && (await Json(uploaded)).GetProperty("url").GetString()!.StartsWith("https://res.cloudinary.com/"), "B22 authenticated image upload returns URL");
var formMember = new Dictionary<string,string> { ["fullName"] = "Image Member", ["phone"] = "0900000001", ["email"] = memberAccount.Email, ["dob"] = "2000-01-01", ["trainingGoal"] = "Goal" };
Check((await http.PutAsync("/api/members/me", ImageForm(formMember))).IsSuccessStatusCode, "B22 Member me accepts multipart avatar file");
Check((await Json(await http.GetAsync("/api/members/me"))).GetProperty("avatar").GetString() == TestMedia.Url, "B22 Member avatar URL persisted");
Login(manager);
var formCoach = new Dictionary<string,string> { ["fullName"] = "Image Coach", ["phone"] = "0900000003", ["email"] = coachAccount.Email, ["experienceYears"] = "3" };
Check((await http.PutAsync($"/api/coaches/{coach.Id}", ImageForm(formCoach))).IsSuccessStatusCode, "B22 Coach multipart update");
Check((await Json(await http.GetAsync($"/api/coaches/{coach.Id}"))).GetProperty("avatar").GetString() == TestMedia.Url, "B22 Coach avatar persisted");
var formReception = new Dictionary<string,string> { ["fullName"] = "Image Reception", ["phone"] = "0900000004", ["email"] = receptionistAccount.Email };
Check((await http.PutAsync($"/api/receptionists/{receptionist.Id}", ImageForm(formReception))).IsSuccessStatusCode, "B22 Receptionist multipart update");
Check((await Json(await http.GetAsync($"/api/receptionists/{receptionist.Id}"))).GetProperty("avatar").GetString() == TestMedia.Url, "B22 Receptionist avatar persisted");
var managerProfileId = Guid.NewGuid();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.CenterManagers.Add(new CenterManager { Id = managerProfileId, AccountId = manager.Id, FullName = "Manager", Phone = "0900000099" }); await db.SaveChangesAsync();
}
var formManager = new Dictionary<string,string> { ["fullName"] = "Image Manager", ["phone"] = "0900000099", ["email"] = manager.Email };
Check((await http.PutAsync($"/api/center-managers/{managerProfileId}", ImageForm(formManager))).IsSuccessStatusCode, "B22 Manager multipart update");
Check((await Json(await http.GetAsync($"/api/center-managers/{managerProfileId}"))).GetProperty("avatar").GetString() == TestMedia.Url, "B22 Manager avatar persisted");
Check((await http.GetAsync("/swagger/v1/swagger.json")).IsSuccessStatusCode, "B22 Swagger works with JSON and multipart actions");
using (var formNoImage = new MultipartFormDataContent()) {
    foreach (var pair in formManager) formNoImage.Add(new StringContent(pair.Value), pair.Key);
    Check((await http.PutAsync($"/api/center-managers/{managerProfileId}", formNoImage)).IsSuccessStatusCode &&
        (await Json(await http.GetAsync($"/api/center-managers/{managerProfileId}"))).GetProperty("avatar").GetString() == TestMedia.Url,
        "B22 multipart without new file preserves existing URL");
}
// FIX B52: kiểm tra chuỗi JSON thật trên mọi đường trả ngày Session/Attendance.
var b52Date = new DateOnly(2026, 10, 15);
var b52Room = new Room { Id = Guid.NewGuid(), RoomName = "B52 Room", Capacity = 20, Status = RoomStatus.Available };
var b52Class = new Class { Id = Guid.NewGuid(), ClassName = "B52 Class", CoachId = coach.Id,
    CategoryId = category.Id, MaxCapacity = 20, Status = ClassStatus.Ongoing,
    StartDate = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
    EndDate = new DateTime(2026, 10, 31, 0, 0, 0, DateTimeKind.Utc) };
var b52Session = new ClassSession { Id = Guid.NewGuid(), Class = b52Class, Room = b52Room,
    Date = b52Date, StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9) };
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.AddRange(b52Session, new ClassBooking { Id = Guid.NewGuid(), Class = b52Class,
        MemberId = member.Id, Status = BookingStatus.Booked });
    await db.SaveChangesAsync();
}
void CheckB52(JsonElement row, string field, string route) {
    Check(row.GetProperty(field).GetString() == "2026-10-15T00:00:00Z" &&
        row.GetProperty(field).GetDateTime().Kind == DateTimeKind.Utc, "B52 explicit UTC calendar date " + route);
}
http.DefaultRequestHeaders.Authorization = null;
CheckB52((await Json(await http.GetAsync("/api/class-sessions"))).EnumerateArray()
    .Single(x => x.GetProperty("id").GetGuid() == b52Session.Id), "date", "all sessions");
CheckB52((await Json(await http.GetAsync($"/api/class-sessions/class/{b52Class.Id}")))[0], "date", "sessions by class");
CheckB52(await Json(await http.GetAsync($"/api/class-sessions/{b52Session.Id}")), "date", "session detail");
Login(coachAccount);
var b52Recorded = await http.PostAsJsonAsync("/api/attendances", new { sessionId = b52Session.Id, memberId = member.Id, status = 1 });
Check(b52Recorded.IsSuccessStatusCode, "B52 record attendance fixture");
CheckB52(await Json(b52Recorded), "sessionDate", "record attendance response");
CheckB52((await Json(await http.GetAsync($"/api/attendances/session/{b52Session.Id}")))[0], "sessionDate", "attendance by session");
Login(memberAccount);
CheckB52((await Json(await http.GetAsync("/api/members/me/attendances"))).EnumerateArray()
    .Single(x => x.GetProperty("sessionId").GetGuid() == b52Session.Id), "sessionDate", "member attendance history");
using (var scope = app.Services.CreateScope()) {
    Check((await scope.ServiceProvider.GetRequiredService<AppDbContext>().ClassSessions.FindAsync(b52Session.Id))!.Date == b52Date,
        "B52 stored DateOnly unchanged");
}
using (var schema = new AppDbContextFactory().CreateDbContext(Array.Empty<string>())) {
    // FIX B52: provider PostgreSQL có thể biên dịch projection, không cần kết nối DB.
    var sql = schema.ClassSessions.Select(cs => new {
        cs.Id, Date = DateTime.SpecifyKind(cs.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
    }).ToQueryString();
    Check(sql.Contains("class_session"), "B52 PostgreSQL date projection compiles");
}
// FIX B45 B46 B47: dữ liệu riêng, không phụ thuộc các thẻ đã cancel/renew trong test cũ.
var wfAccount = Account(AccountRole.Member, "workflow_member");
var wfMember = new Member { Id = Guid.NewGuid(), Account = wfAccount, FullName = "Workflow", Phone = "0990000001" };
var wfClass = new Class { Id = Guid.NewGuid(), ClassName = "Workflow class", CategoryId = category.Id, CoachId = coach.Id,
    MaxCapacity = 20, StartDate = new DateTime(2026,10,1,0,0,0,DateTimeKind.Utc), EndDate = new DateTime(2026,12,1,0,0,0,DateTimeKind.Utc) };
var wfSession = new ClassSession { Id = Guid.NewGuid(), ClassId = wfClass.Id, Date = new DateOnly(2026,10,24), StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9) };
var wfCardId = Guid.NewGuid();
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.AddRange(wfMember, wfClass, wfSession); await db.SaveChangesAsync();
}
Login(wfAccount);
Check((await http.PostAsJsonAsync("/api/class-bookings", new { memberId = wfMember.Id, classId = wfClass.Id })).StatusCode == HttpStatusCode.BadRequest, "B45 member without card cannot book");
Login(receptionistAccount);
Check((await http.PostAsJsonAsync("/api/class-bookings", new { memberId = wfMember.Id, classId = wfClass.Id })).StatusCode == HttpStatusCode.BadRequest, "B45 receptionist cannot bypass card");
Check((await http.PostAsJsonAsync("/api/check-ins", new { memberId = wfMember.Id })).StatusCode == HttpStatusCode.BadRequest, "B47 no card cannot check in");
Login(manager);
var wfPackageHttp = await http.PostAsJsonAsync("/api/membership-packages", new { packageName = "Restricted", description = "Fitness only", durationDays = 30, price = 100, status = 1, categoryIds = new[] { category.Id, category.Id } });
Check(wfPackageHttp.IsSuccessStatusCode, "B46 create category-restricted package");
var wfPackageJson = await Json(wfPackageHttp); var wfPackageId = wfPackageJson.GetProperty("membershipPackageId").GetGuid();
Check(wfPackageJson.GetProperty("categoryIds").GetArrayLength() == 1, "B46 category IDs deduplicated");
Check((await http.PostAsJsonAsync("/api/membership-packages", new { packageName = "Invalid categories", durationDays = 1, status = 1, categoryIds = new[] { Guid.NewGuid() } })).StatusCode == HttpStatusCode.BadRequest, "B46 missing category rejected");
var wfWrongCategory = new SportCategory { Id = Guid.NewGuid(), CategoryName = "Wrong sport" };
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Add(wfWrongCategory); db.Memberships.Add(new Membership { Id = wfCardId, MemberId = wfMember.Id, PackageId = wfPackageId, Status = MembershipStatus.Active,
        StartDate = new DateTime(2026,10,1,0,0,0,DateTimeKind.Utc), EndDate = new DateTime(2026,10,23,0,0,0,DateTimeKind.Utc) }); await db.SaveChangesAsync();
}
Login(receptionistAccount);
Check((await http.PostAsJsonAsync("/api/class-bookings", new { memberId = wfMember.Id, classId = wfClass.Id })).StatusCode == HttpStatusCode.BadRequest, "B45 card must cover next session not just today");
var wfIn = await http.PostAsJsonAsync("/api/check-ins", new { memberId = wfMember.Id });
Check(wfIn.StatusCode == HttpStatusCode.Created && (await Json(wfIn)).GetProperty("daysRemaining").GetInt32() == 0, "B47 card valid on entire final local day");
Check((await Json(wfIn)).GetProperty("receptionistId").GetGuid() == receptionist.Id && (await Json(wfIn)).GetProperty("recordedByAccountId").GetGuid() == receptionistAccount.Id, "B47 check-in actor from token");
Check((await Json(await http.GetAsync("/api/check-ins?date=2026-10-23"))).GetProperty("totalItems").GetInt32() == 1, "B47 filter local day");
Check((await Json(await http.GetAsync("/api/check-ins?date=2026-10-24"))).GetProperty("totalItems").GetInt32() == 0, "B47 exclude next day");
// FIX B45 B47: thẻ inactive/cancelled/xóa mềm/chưa bắt đầu/hết hạn đều không được sử dụng.
foreach (var invalidCard in new[] { "inactive", "cancelled", "deleted", "future", "expired" }) {
    using (var scope = app.Services.CreateScope()) {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var card = (await db.Memberships.FindAsync(wfCardId))!;
        card.Status = invalidCard == "inactive" ? MembershipStatus.Inactive : invalidCard == "cancelled" ? MembershipStatus.Cancelled : MembershipStatus.Active;
        card.IsDeleted = invalidCard == "deleted";
        card.StartDate = new DateTime(2026,10,invalidCard == "future" ? 25 : 1,0,0,0,DateTimeKind.Utc);
        card.EndDate = new DateTime(2026,10,invalidCard == "expired" ? 22 : 24,0,0,0,DateTimeKind.Utc);
        await db.SaveChangesAsync();
    }
    Check((await http.PostAsJsonAsync("/api/check-ins", new { memberId = wfMember.Id })).StatusCode == HttpStatusCode.BadRequest, "B47 reject card " + invalidCard);
    Check((await http.PostAsJsonAsync("/api/class-bookings", new { memberId = wfMember.Id, classId = wfClass.Id })).StatusCode == HttpStatusCode.BadRequest, "B45 reject card " + invalidCard);
}
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var card = (await db.Memberships.FindAsync(wfCardId))!;
    card.Status = MembershipStatus.Active; card.IsDeleted = false;
    card.StartDate = new DateTime(2026,10,1,0,0,0,DateTimeKind.Utc); card.EndDate = new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc); await db.SaveChangesAsync();
}
Login(wfAccount);
Check((await Json(await http.GetAsync("/api/members/me/check-ins"))).GetProperty("totalItems").GetInt32() == 1, "B47 member sees own history");
Check((await http.PostAsJsonAsync("/api/check-ins", new { memberId = wfMember.Id })).StatusCode == HttpStatusCode.Forbidden, "B47 member cannot self check in");
Check((await http.GetAsync("/api/check-ins")).StatusCode == HttpStatusCode.Forbidden, "B47 member cannot view all check-ins");
Login(memberAccount);
Check((await Json(await http.GetAsync("/api/members/me/check-ins"))).GetProperty("totalItems").GetInt32() == 0, "B47 history isolation");
Login(coachAccount);
Check((await http.PostAsJsonAsync("/api/check-ins", new { memberId = wfMember.Id })).StatusCode == HttpStatusCode.Forbidden, "B47 coach cannot check in");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var card = (await db.Memberships.FindAsync(wfCardId))!;
    card.EndDate = new DateTime(2026,10,24,0,0,0,DateTimeKind.Utc); await db.SaveChangesAsync();
}
Login(manager);
Check((await http.PutAsJsonAsync($"/api/membership-packages/{wfPackageId}", new { packageName = "Restricted", durationDays = 30, price = 100, status = 1, categoryIds = new[] { wfWrongCategory.Id } })).IsSuccessStatusCode, "B46 change allowed sport");
Login(wfAccount);
Check((await http.PostAsJsonAsync("/api/class-bookings", new { classId = wfClass.Id })).StatusCode == HttpStatusCode.BadRequest, "B46 wrong sport rejected");
Login(manager);
Check((await http.PostAsJsonAsync("/api/check-ins", new { memberId = wfMember.Id })).StatusCode == HttpStatusCode.Created, "B47 manager check-in ignores sport restriction");
var wfUpdatePackage = new { packageName = "All sports", durationDays = 30, price = 100, status = 1, categoryIds = Array.Empty<Guid>() };
Check((await http.PutAsJsonAsync($"/api/membership-packages/{wfPackageId}", wfUpdatePackage)).IsSuccessStatusCode, "B46 empty categories allow all sports");
Login(wfAccount);
var wfBooked = await http.PostAsJsonAsync("/api/class-bookings", new { classId = wfClass.Id });
Check(wfBooked.IsSuccessStatusCode && (await Json(wfBooked)).GetProperty("eligibleMembershipId").GetGuid() == wfCardId, "B45 valid card books and reports selected membership");
Check((await Json(wfBooked)).GetProperty("membershipEndDate").GetDateTime() < wfClass.EndDate, "B45 card need not cover entire class");

// FIX B49: một SaveChanges cho các học viên hợp lệ, bỏ booking hủy và chống coach lớp khác.
var wfExtra = new Member { Id = Guid.NewGuid(), Account = Account(AccountRole.Member, "workflow_extra"), FullName = "Extra", Phone = "0990000003" };
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(wfExtra);
    db.ClassBookings.AddRange(new ClassBooking { Id = Guid.NewGuid(), ClassId = wfClass.Id, MemberId = wfExtra.Id, Status = BookingStatus.Booked },
        new ClassBooking { Id = Guid.NewGuid(), ClassId = wfClass.Id, MemberId = member.Id, Status = BookingStatus.Cancelled },
        new ClassBooking { Id = Guid.NewGuid(), ClassId = wfClass.Id, MemberId = other.Id, Status = BookingStatus.Booked, IsDeleted = true });
    await db.SaveChangesAsync();
}
Login(manager);
Check((await http.PostAsJsonAsync($"/api/training-plans/class/{wfClass.Id}", new { title = "Shared", contentDescription = "Practice" })).StatusCode == HttpStatusCode.Forbidden, "B49 manager cannot create coach plan");
Login(coachAccount);
var savesBeforeBatch = workflowSaves.Count;
var wfBulk = await http.PostAsJsonAsync($"/api/training-plans/class/{wfClass.Id}", new { title = "Shared", contentDescription = "Practice" });
Check(wfBulk.IsSuccessStatusCode && (await Json(wfBulk)).GetProperty("createdCount").GetInt32() == 2, "B49 class plans created only for booked active members");
Check(workflowSaves.Count - savesBeforeBatch == 1, "B49 class batch uses one SaveChanges including audit");
var wfPlanId = (await Json(wfBulk)).GetProperty("data").EnumerateArray().Single(p => p.GetProperty("memberId").GetGuid() == wfMember.Id).GetProperty("trainingPlanId").GetGuid();
Check((await http.PostAsJsonAsync($"/api/training-plans/class/{wfClass.Id}", new { title = " " })).StatusCode == HttpStatusCode.BadRequest, "B49 empty title rejected before saving");
var wfAlienCoachAccount = Account(AccountRole.Coach, "workflow_other_coach");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(new Coach { Id = Guid.NewGuid(), Account = wfAlienCoachAccount, FullName = "Other coach", Phone = "0990000002" }); await db.SaveChangesAsync();
}
Login(wfAlienCoachAccount);
Check((await http.PostAsJsonAsync($"/api/training-plans/class/{wfClass.Id}", new { title = "Attack" })).StatusCode == HttpStatusCode.Forbidden, "B49 other coach cannot create class plans");

// FIX B51: Session phải đúng lớp và có Attendance Present/Late, null giữ tương thích cũ.
Login(coachAccount);
object WfResult(Guid? sessionId) => new { planId = wfPlanId, memberId = wfMember.Id, coachId = coach.Id, sessionId, performanceScore = 8, coachComment = "Good", evaluationDate = "2026-10-24T00:00:00Z" };
Check((await http.PostAsJsonAsync("/api/training-results", WfResult(session.Id))).StatusCode == HttpStatusCode.BadRequest, "B51 wrong-class session denied");
Check((await http.PostAsJsonAsync("/api/training-results", WfResult(wfSession.Id))).StatusCode == HttpStatusCode.BadRequest, "B51 unrecorded attendance denied");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Attendances.Add(new Attendance { Id = Guid.NewGuid(), SessionId = wfSession.Id, MemberId = wfMember.Id, Status = AttendanceStatus.Late, RecordedBy = coachAccount.Id }); await db.SaveChangesAsync();
}
var wfResultHttp = await http.PostAsJsonAsync("/api/training-results", WfResult(wfSession.Id));
Check(wfResultHttp.IsSuccessStatusCode && (await Json(wfResultHttp)).GetProperty("sessionId").GetGuid() == wfSession.Id, "B51 late attendee result linked to session");
var wfResultId = (await Json(wfResultHttp)).GetProperty("trainingResultId").GetGuid();
Check((await http.PutAsJsonAsync($"/api/training-results/{wfResultId}", WfResult(session.Id))).StatusCode == HttpStatusCode.BadRequest, "B51 update cannot move result to wrong class session");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var row = await db.Attendances.SingleAsync(a => a.SessionId == wfSession.Id && a.MemberId == wfMember.Id);
    row.Status = AttendanceStatus.Absent; await db.SaveChangesAsync();
}
Check((await http.PutAsJsonAsync($"/api/training-results/{wfResultId}", WfResult(wfSession.Id))).StatusCode == HttpStatusCode.BadRequest, "B51 absent attendee update denied");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Check((await db.TrainingResults.FindAsync(wfResultId))!.SessionId == wfSession.Id, "B51 failed update preserves saved session");
}
Check((await http.PostAsJsonAsync("/api/training-results", WfResult(null))).IsSuccessStatusCode, "B51 legacy general result remains supported");

// FIX B50: chỉ Manager chuyển người KHÁC, hồ sơ lịch sử giữ ID và role cũ hết quyền.
object WfRoleBody(Account account, AccountRole role) => new { username = account.Username, email = account.Email, fullName = "Reception promoted", phone = "0900000004", status = 1, role };
Login(receptionistAccount);
Check((await http.PutAsJsonAsync($"/api/accounts/{receptionistAccount.Id}", WfRoleBody(receptionistAccount, AccountRole.CenterManager))).StatusCode == HttpStatusCode.Forbidden, "B50 receptionist cannot promote self");
Login(coachAccount);
Check((await http.PutAsJsonAsync($"/api/accounts/{receptionistAccount.Id}", WfRoleBody(receptionistAccount, AccountRole.CenterManager))).StatusCode == HttpStatusCode.Forbidden, "B50 coach cannot promote receptionist");
Login(manager);
Check((await http.PutAsJsonAsync($"/api/accounts/{manager.Id}", WfRoleBody(manager, AccountRole.Receptionist))).StatusCode == HttpStatusCode.Forbidden, "B50 manager cannot change own role");
var wfPromote = await http.PutAsJsonAsync($"/api/accounts/{receptionistAccount.Id}", WfRoleBody(receptionistAccount, AccountRole.CenterManager));
if (!wfPromote.IsSuccessStatusCode) Console.WriteLine($"DEBUG B50 {wfPromote.StatusCode} {await wfPromote.Content.ReadAsStringAsync()}");
Check(wfPromote.IsSuccessStatusCode, "B50 manager promotes receptionist");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var a = await db.Accounts.Include(x => x.Receptionist).Include(x => x.CenterManager).SingleAsync(x => x.Id == receptionistAccount.Id);
    Check(a.Role == AccountRole.CenterManager && a.Receptionist!.IsDeleted && a.CenterManager != null && !a.CenterManager.IsDeleted, "B50 active manager profile and soft-deleted receptionist");
    Check(a.Receptionist!.Id == receptionist.Id && await db.Set<CheckIn>().AnyAsync(x => x.ReceptionistId == receptionist.Id), "B50 old receptionist ID and check-in history preserved");
    Check(a.FullName == "Reception promoted" && a.Phone == "0900000004", "B50 old profile does not overwrite new account contact");
    var options = app.Services.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>().Get("Bearer");
    var ctx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
    async Task<bool> WfValidateRole(string role) {
        var tokenContext = new Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext(ctx,
            new AuthenticationScheme("Bearer", null, typeof(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerHandler)), options) {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, a.Id.ToString()), new Claim(ClaimTypes.Role, role) }, "Bearer")) };
        await options.Events.TokenValidated(tokenContext); return tokenContext.Result?.Failure == null;
    }
    Check(!await WfValidateRole(nameof(AccountRole.Receptionist)), "B50 production JWT rejects previous role");
    Check(await WfValidateRole(nameof(AccountRole.CenterManager)), "B50 production JWT accepts current role");
}
Check(!(await Json(await http.GetAsync("/api/receptionists"))).EnumerateArray().Any(x => x.GetProperty("accountId").GetGuid() == receptionistAccount.Id), "B50 promoted account removed from receptionist list");
Check((await http.PutAsJsonAsync($"/api/accounts/{receptionistAccount.Id}", WfRoleBody(receptionistAccount, AccountRole.Receptionist))).IsSuccessStatusCode, "B50 manager can restore previous role");
using (var scope = app.Services.CreateScope()) {
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Check(await db.Receptionists.CountAsync(x => x.AccountId == receptionistAccount.Id && !x.IsDeleted) == 1 &&
        (await db.Receptionists.SingleAsync(x => x.AccountId == receptionistAccount.Id)).Id == receptionist.Id, "B50 restore old profile without duplicate account FK");
}
// FIX B45 B47: kiểm tra chính query dùng ở production biên dịch được bằng provider Npgsql.
using (var schema = new AppDbContextFactory().CreateDbContext(Array.Empty<string>())) {
    var eligibilitySql = SWP_SportCenter.Service.Membership.MembershipEligibility.Query(schema, wfMember.Id, new DateOnly(2026,10,24), category.Id).ToQueryString();
    Check(eligibilitySql.Contains("membership_package_category"), "B45 B46 PostgreSQL membership predicate compiles");
    using var scope = app.Services.CreateScope();
    var service = new SWP_SportCenter.Service.CheckIns.CheckInService(schema, scope.ServiceProvider.GetRequiredService<CurrentActor>(), new FixedClock(bookingNow));
    var query = (IQueryable<SWP_SportCenter.Service.CheckIns.CheckInResponse>)typeof(SWP_SportCenter.Service.CheckIns.CheckInService)
        .GetMethod("Query", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.Invoke(service, null)!;
    Check(query.ToQueryString().Contains("check_in"), "B47 PostgreSQL check-in projection compiles");
    var migrationSql = schema.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().GenerateScript("20261025134337_SecurePaymentsAndProfileImages");
    Check(migrationSql.Contains("CREATE TABLE check_in") && migrationSql.Contains("membership_package_category") && migrationSql.Contains("SessionId"), "B46 B47 B51 additive migration SQL generated without connection");
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

// FIX B49: chỉ đếm, không thay đổi hành vi lưu dữ liệu.
sealed class WorkflowSaveCounter : Microsoft.EntityFrameworkCore.Diagnostics.SaveChangesInterceptor
{
    public int Count;
    public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int>> SavingChangesAsync(
        Microsoft.EntityFrameworkCore.Diagnostics.DbContextEventData data, Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Interlocked.Increment(ref Count); return ValueTask.FromResult(result); }
}

// FIX B22: cloud fake riêng trong test, không đưa credential vào repo hoặc gọi Cloudinary thật.
sealed class TestMedia : SWP_SportCenter.Service.MediaService.IService
{
    public const string Url = "https://res.cloudinary.com/test/image/upload/avatar.png";
    public async Task<string> UploadAsync(IFormFile file) {
        await SWP_SportCenter.Service.MediaService.ImageValidation.ValidateAsync(file);
        return Url;
    }
    public Task<string> UploadVideoAsync(IFormFile file) => throw new NotSupportedException();
}
