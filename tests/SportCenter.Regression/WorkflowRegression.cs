using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
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
using SWP_SportCenter.Service.Ai;

// FIX: workflow hiện hành B53 B55 B56 B58, provider HTTP giả, không chạm DB/tiền/AI thật.
internal static class WorkflowRegression
{
    public static async Task RunAsync()
    {
        var builder = WebApplication.CreateBuilder(Array.Empty<string>());
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
        var bookingNow = new DateTimeOffset(2026, 10, 29, 3, 0, 0, TimeSpan.Zero);
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
        // FIX SEPAY: mặc định tắt như production chưa cấu hình, bật riêng phần test gateway bên dưới.
        builder.Services.Configure<SWP_SportCenter.Service.Sepay.SePayOptions>(builder.Configuration.GetSection("SePay"));
        builder.Services.AddScoped<SWP_SportCenter.Service.Sepay.ISepayService, SWP_SportCenter.Service.Sepay.SepayService>();
        // FIX B22 B39: test upload giả lập không dùng secret/cloud thật, expiration dùng clock cố định.
        builder.Services.AddScoped<SWP_SportCenter.Service.MediaService.IService, TestMedia>();
        builder.Services.AddScoped<SWP_SportCenter.Service.Receptionist.IReceptionistService, SWP_SportCenter.Service.Receptionist.ReceptionistService>();
        builder.Services.AddScoped<SWP_SportCenter.Service.CenterManager.ICenterManagerService, SWP_SportCenter.Service.CenterManager.CenterManagerService>();
        builder.Services.AddScoped<SWP_SportCenter.Service.Payment.PaymentExpirationService>();
        builder.Services.AddScoped<SWP_SportCenter.Service.CheckIns.CheckInService>(); // FIX B47
        
        builder.Services.AddScoped<SWP_SportCenter.Service.Payment.ClassCommerceService>();
        builder.Services.AddScoped<SWP_SportCenter.Service.Classes.ClassLifecycleService>();
        builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai"));
        var aiHttp = new AiStubHandler();
        // CODE CŨ: GeminiClient chỉ nhận HttpClient/options, không có log theo traceId.
        builder.Services.AddScoped<IAiClient>(sp => new GeminiClient(new HttpClient(aiHttp, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan },
            sp.GetRequiredService<IOptionsSnapshot<AiOptions>>(), sp.GetRequiredService<ILogger<GeminiClient>>(), sp.GetRequiredService<IHttpContextAccessor>()));
        builder.Services.AddScoped<AiService>();
        builder.Services.AddRateLimiter(o => { o.RejectionStatusCode = 429;
            o.OnRejected = SWP_SportCenter.Infrastructure.AiRateLimitResponse.WriteAsync; // FIX: cùng contract với production.
            o.AddPolicy("ai", c => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anon",
                _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 })); });
        builder.Configuration["SePay:Enabled"] = "true"; builder.Configuration["SePay:BankBin"] = "970422";
        builder.Configuration["SePay:AccountNumber"] = "000000000000"; builder.Configuration["SePay:Gateway"] = "MBBank";
        builder.Configuration["SePay:WebhookApiKey"] = "test-only-sepay-webhook-key-not-a-production-secret-2026";
        var app = builder.Build(); app.UseMiddleware<SWP_SportCenter.Middleware.ApiExceptionMiddleware>(); app.UseSwaggerAPI();
        using var staticFiles = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../SWP_SportCenter.Api/wwwroot")));
        app.UseStaticFiles(new StaticFileOptions { FileProvider = staticFiles });
        app.UseAuthentication(); app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers(); await app.StartAsync();
        using var http = new HttpClient { BaseAddress = new Uri(app.Services.GetRequiredService<Microsoft.AspNetCore.Hosting.Server.IServer>()
            .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>()!.Addresses.Single()) };
        int passed = 0;
        void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL " + name); Console.WriteLine("PASS " + name); passed++; }
        async Task<JsonElement> Json(HttpResponseMessage r) => JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement.Clone();
        void Login(Account a) => http.DefaultRequestHeaders.Authorization = new("Test", a.Id + "|" + a.Role);
        Account Account(AccountRole role, string name) => new() { Id = Guid.NewGuid(), Username = name, Email = name + "@test.local", FullName = name, Role = role, Status = AccountStatus.Active };
        var manager = Account(AccountRole.CenterManager, "manager"); var receptionistAccount = Account(AccountRole.Receptionist, "reception");
        var coachAccount = Account(AccountRole.Coach, "coach"); var otherCoachAccount = Account(AccountRole.Coach, "other_coach");
        var memberAccount = Account(AccountRole.Member, "no_card"); var cardAccount = Account(AccountRole.Member, "has_card"); var quotaAccount = Account(AccountRole.Member, "quota");
        var member = new Member { Id = Guid.NewGuid(), Account = memberAccount, FullName = "Member no card", Phone = "0900000001" };
        var cardMember = new Member { Id = Guid.NewGuid(), Account = cardAccount, FullName = "Member card", Phone = "0900000002", TrainingGoal = "Build strength" };
        var quotaMember = new Member { Id = Guid.NewGuid(), Account = quotaAccount, FullName = "Quota", Phone = "0900000005" };
        var coach = new Coach { Id = Guid.NewGuid(), Account = coachAccount, FullName = "Coach", Phone = "0900000003" };
        var otherCoach = new Coach { Id = Guid.NewGuid(), Account = otherCoachAccount, FullName = "Other coach", Phone = "0900000006" };
        var receptionist = new Receptionist { Id = Guid.NewGuid(), Account = receptionistAccount, FullName = "Receptionist", Phone = "0900000004" };
        var category = new SportCategory { Id = Guid.NewGuid(), CategoryName = "Gym" };
        var month = new MembershipPackage { Id = Guid.NewGuid(), PackageName = "Gói 1 tháng", DurationDays = 30, Price = 800000, Status = MembershipStatus.Active, DiscountPercent = 10, Benefits = "Tự tập\nGiảm học phí" };
        var six = new MembershipPackage { Id = Guid.NewGuid(), PackageName = "Gói 6 tháng", DurationDays = 187, Price = 4080000, Status = MembershipStatus.Active, DiscountPercent = 10 };
        var today = new DateTime(2026,10,29,0,0,0,DateTimeKind.Utc);
        var card = new Membership { Id = Guid.NewGuid(), Member = cardMember, Package = month, StartDate = today.AddDays(-9), EndDate = today.AddDays(20), Status = MembershipStatus.Active };
        card.Invoice = new Invoice { Id = Guid.NewGuid(), Membership = card, Amount = 800000, OriginalAmount = 800000, Status = InvoiceStatus.Paid, PaymentMethod = PaymentMethod.Cash, PaymentDate = bookingNow.AddDays(-9), ReceptionistId = receptionist.Id };
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.AddRange(manager, receptionist, coach, otherCoach, member, card, quotaMember, category, six); await db.SaveChangesAsync(); }
        async Task<Class> NewClass(string name, decimal price = 100000, int capacity = 10) {
            var c = new Class { Id = Guid.NewGuid(), ClassName = name, CategoryId = category.Id, CoachId = coach.Id, Price = price,
                MaxCapacity = capacity, StartDate = today.AddDays(1), EndDate = today.AddDays(7), Status = ClassStatus.Scheduled };
            c.ClassSessions.Add(new ClassSession { Id = Guid.NewGuid(), Class = c, Date = DateOnly.FromDateTime(today).AddDays(1), StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(9) });
            using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(c); await db.SaveChangesAsync(); return c;
        }
        async Task<JsonElement> ClassCheckout(Class c, Account a) { Login(a); var r = await http.PostAsJsonAsync("/api/payments/class-checkout", new { classId = c.Id, paymentMethod = 2 });
            if (!r.IsSuccessStatusCode) Console.WriteLine(await r.Content.ReadAsStringAsync()); Check(r.IsSuccessStatusCode, "B53 class-checkout succeeds"); return await Json(r); }
        long eventId = 10000;
        async Task<HttpResponseMessage> Pay(JsonElement order, decimal? amount = null, bool key = true) {
            using var client = new HttpClient { BaseAddress = http.BaseAddress };
            if (key) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Apikey test-only-sepay-webhook-key-not-a-production-secret-2026");
            return await client.PostAsJsonAsync("/api/sepay/webhook", new { id = ++eventId, content = order.GetProperty("paymentReference").GetString(), transferAmount = amount ?? order.GetProperty("amount").GetDecimal(),
                referenceCode = "BANK" + eventId, transactionDate = "2026-10-29 10:00:00", gateway = "MBBank", transferType = "in", accountNumber = "000000000000" });
        }
        Check((await http.GetAsync("/swagger/v1/swagger.json")).IsSuccessStatusCode, "Swagger includes SePay and AI actions");
        var chatPage = await http.GetAsync("/ai-chat.html");
        Check(chatPage.IsSuccessStatusCode && (await chatPage.Content.ReadAsStringAsync()).Contains("/api/ai/"), "B48 simple chat page served without exposing AI key");
        Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" })).StatusCode == HttpStatusCode.Unauthorized, "B48 anonymous chat forbidden");
        var opts = await Json(await http.GetAsync("/api/payments/options"));
        Check(opts.GetProperty("provider").GetString() == "SePay" && opts.GetProperty("holdMinutes").GetInt32() == 15, "No MoMo provider advertised");
        var paidClass = await NewClass("Paid course");
        var order = await ClassCheckout(paidClass, memberAccount); var paymentId = order.GetProperty("paymentId").GetGuid(); var bookingId = order.GetProperty("bookingId").GetGuid();
        Check(order.GetProperty("amount").GetDecimal() == 100000 && order.GetProperty("discountPercent").GetInt32() == 0, "B53 no membership pays full price");
        Check(order.GetProperty("kind").GetString() == "Class" && order.GetProperty("membershipId").ValueKind == JsonValueKind.Null, "B53 invoice subject Class not fake Membership");
        Check(order.GetProperty("expiresAt").GetDateTimeOffset() == bookingNow.AddMinutes(15), "B53 hold lasts 15 minutes");
        var dup = await http.PostAsJsonAsync("/api/payments/class-checkout", new { classId = paidClass.Id });
        Check(dup.StatusCode == HttpStatusCode.Conflict && (await Json(dup)).GetProperty("paymentId").GetGuid() == paymentId, "B53 duplicate hold returns existing paymentId");
        var detail = await Json(await http.GetAsync($"/api/payments/{paymentId}"));
        Check(detail.GetProperty("classId").GetGuid() == paidClass.Id && detail.GetProperty("status").GetInt32() == 1, "B53 payment detail handles nullable Membership");
        var invoices = await Json(await http.GetAsync("/api/members/me/invoices"));
        Check(invoices.EnumerateArray().Any(i => i.GetProperty("classBookingId").GetGuid() == bookingId), "B53 member invoices include class fee");
        Login(cardAccount);
        Check((await http.GetAsync($"/api/payments/{paymentId}")).StatusCode == HttpStatusCode.Forbidden, "B53 payment instructions owner scoped");
        var discountOrder = await ClassCheckout(paidClass, cardAccount);
        Check(discountOrder.GetProperty("amount").GetDecimal() == 90000 && discountOrder.GetProperty("discountPercent").GetInt32() == 10, "B53 active card discounts all class tuition");
        Login(coachAccount);
        var unpaidSessionId = paidClass.ClassSessions.Single().Id;
        Check((await http.PostAsJsonAsync("/api/attendances", new { sessionId = unpaidSessionId, memberId = member.Id, status = 1 })).StatusCode == HttpStatusCode.BadRequest, "B53 unpaid hold cannot be marked present");
        Login(receptionistAccount);
        var roster = await Json(await http.GetAsync($"/api/classes/{paidClass.Id}/members"));
        Check(roster.GetArrayLength() == 0, "B53 unpaid holds excluded from roster");
        Check((await http.PutAsJsonAsync($"/api/invoices/{paymentId}", new { status = 2, paymentMethod = 2 })).StatusCode == HttpStatusCode.Conflict, "B43 online invoice cannot be forced Paid through PUT");
        Check((await http.GetAsync($"/api/invoices/invoices/{paymentId}/export")).StatusCode == HttpStatusCode.NotFound, "B11 wrong legacy export route removed");
        Check((await http.GetAsync($"/api/invoices/{paymentId}/export")).IsSuccessStatusCode, "B11 correct export route works for Receptionist");
        Check((await Pay(order, key: false)).StatusCode == HttpStatusCode.Unauthorized, "SePay forged callback denied");
        Check((await Pay(order, 99999)).IsSuccessStatusCode, "SePay mismatch receipt accepted for reconciliation only");
        Login(memberAccount);
        Check((await Json(await http.GetAsync($"/api/payments/{paymentId}"))).GetProperty("status").GetInt32() == 1, "SePay underpayment leaves class Pending");
        Check((await Pay(order)).IsSuccessStatusCode && (await Pay(discountOrder)).IsSuccessStatusCode, "SePay settles full and discounted class orders");
        Check((await Json(await http.GetAsync($"/api/payments/{paymentId}"))).GetProperty("status").GetInt32() == 2, "B53 callback books class not membership");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Check((await db.ClassBookings.FindAsync(bookingId))!.Status == BookingStatus.Booked && await db.Memberships.CountAsync(m => m.MemberId == member.Id) == 0, "B53 no fake card created for paid class");
            Check(await db.Notifications.AnyAsync(n => n.AccountId == memberAccount.Id && n.Title == "Thanh toán thành công" && n.SenderAccountId == null), "B57 payment notification is system sender"); }
        var cancel = await http.DeleteAsync($"/api/class-bookings/{bookingId}/member/{member.Id}"); var cancelled = await Json(cancel);
        Check(cancel.IsSuccessStatusCode && cancelled.GetProperty("refundPending").GetBoolean() && !cancelled.GetProperty("refunded").GetBoolean(), "B56 early cancel requests manual refund not fake refunded");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Check((await db.Invoices.FindAsync(paymentId))!.Status == InvoiceStatus.Paid && (await db.ClassBookings.FindAsync(bookingId))!.Status == BookingStatus.Cancelled, "B56 finance remains Paid until money actually returned"); }
        Login(manager);
        Check((await http.PostAsJsonAsync($"/api/payments/{paymentId}/refund-confirm", new { reference = "BANK-REFUND" })).StatusCode == HttpStatusCode.Forbidden, "B58 Manager cannot confirm cashier refund");
        Login(receptionistAccount);
        Check((await http.PostAsJsonAsync($"/api/payments/{paymentId}/refund-confirm", new { reference = "" })).StatusCode == HttpStatusCode.BadRequest, "B56 refund confirmation requires reconciliation reference");
        Check((await http.PostAsJsonAsync($"/api/payments/{paymentId}/refund-confirm", new { reference = "BANK-RETURN-1" })).IsSuccessStatusCode, "B56 Receptionist records actual returned funds");
        Check((await http.PostAsJsonAsync($"/api/payments/{paymentId}/refund-confirm", new { reference = "BANK-RETURN-1" })).IsSuccessStatusCode, "B56 refund confirm idempotent");
        Login(memberAccount); var refunded = await Json(await http.GetAsync($"/api/payments/{paymentId}"));
        Check(refunded.GetProperty("status").GetInt32() == 6 && refunded.GetProperty("refundAmount").GetDecimal() == 100000, "B56 payment detail reports real refund");
        var rebook = await ClassCheckout(paidClass, memberAccount);
        Check(rebook.GetProperty("bookingId").GetGuid() != bookingId, "B56 can rebook before start preserving canceled history");
        Check((await http.PostAsync($"/api/payments/{rebook.GetProperty("paymentId").GetGuid()}/cancel", null)).IsSuccessStatusCode, "B56 owner cancels unpaid order and releases seat");
        Check((await Pay(rebook)).IsSuccessStatusCode, "SePay late incoming on canceled order retained for manual reconciliation");
        Check((await Json(await http.GetAsync("/api/payments/" + rebook.GetProperty("paymentId").GetGuid()))).GetProperty("status").GetInt32() == 4, "SePay canceled order not resurrected");

        var free = await NewClass("Free course", 0);
        var freeOrder = await ClassCheckout(free, memberAccount);
        Check(freeOrder.GetProperty("status").GetInt32() == 2 && freeOrder.GetProperty("qrCodeUrl").ValueKind == JsonValueKind.Null, "B53 free class immediately booked without card or QR");
        var seat = await NewClass("One seat", 100000, 1); var seatOrder = await ClassCheckout(seat, memberAccount);
        Login(cardAccount);
        Check((await http.PostAsJsonAsync("/api/payments/class-checkout", new { classId = seat.Id })).StatusCode == HttpStatusCode.Conflict, "B53 live pending hold occupies capacity");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var i = await db.Invoices.Include(i => i.ClassBooking).SingleAsync(i => i.Id == seatOrder.GetProperty("paymentId").GetGuid()); i.PaymentExpiresAt = bookingNow; i.ClassBooking!.PaymentExpiresAt = bookingNow; await db.SaveChangesAsync(); }
        var replacement = await ClassCheckout(seat, cardAccount);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); await scope.ServiceProvider.GetRequiredService<SWP_SportCenter.Service.Payment.PaymentExpirationService>().ExpireAsync();
            Check((await db.ClassBookings.FindAsync(seatOrder.GetProperty("bookingId").GetGuid()))!.Status == BookingStatus.Cancelled, "B56 expiry worker cancels hold preserving history"); }
        Login(memberAccount);
        Check((await Json(await http.GetAsync("/api/payments/" + seatOrder.GetProperty("paymentId").GetGuid()))).GetProperty("status").GetInt32() == 5, "B56 expired distinct from user cancellation");

        var begun = await NewClass("Begun"); var begunOrder = await ClassCheckout(begun, memberAccount); await Pay(begunOrder);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var s = await db.ClassSessions.SingleAsync(s => s.ClassId == begun.Id); s.Date = DateOnly.FromDateTime(today); s.StartTime = TimeSpan.FromHours(9); s.EndTime = TimeSpan.FromHours(11); await db.SaveChangesAsync(); }
        var noRefund = await Json(await http.DeleteAsync($"/api/class-bookings/{begunOrder.GetProperty("bookingId").GetGuid()}/member/{member.Id}"));
        Check(!noRefund.GetProperty("refundPending").GetBoolean() && !noRefund.GetProperty("refunded").GetBoolean(), "B56 after first start cancellation has no refund");
        Check((await http.PostAsJsonAsync("/api/payments/class-checkout", new { classId = begun.Id })).StatusCode == HttpStatusCode.BadRequest, "B56 elapsed first start blocked despite stale Scheduled status");
        Login(receptionistAccount);
        Check((await http.PostAsJsonAsync("/api/class-bookings", new { memberId = cardMember.Id, classId = begun.Id })).StatusCode == HttpStatusCode.BadRequest, "B56 counter booking also blocked after first start");
        Login(manager);
        Check((await http.DeleteAsync($"/api/classes/{begun.Id}")).StatusCode == HttpStatusCode.BadRequest, "B58 cannot delete running class based on actual schedule");
        Check((await http.DeleteAsync($"/api/classes/{free.Id}")).IsSuccessStatusCode, "B58 can soft-delete scheduled class with booking and invoice history");
        Check((await http.GetAsync($"/api/classes/{free.Id}")).StatusCode == HttpStatusCode.NotFound, "B58 deleted class not public");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Check((await db.Classes.FindAsync(free.Id))!.IsDeleted && await db.Invoices.AnyAsync(i => i.ClassBookingId == freeOrder.GetProperty("bookingId").GetGuid()), "B58 deleting class keeps invoice and booking");
            await scope.ServiceProvider.GetRequiredService<SWP_SportCenter.Service.Classes.ClassLifecycleService>().RefreshAsync();
            Check((await db.Classes.FindAsync(begun.Id))!.Status == ClassStatus.Ongoing, "B56 lifecycle job sets Ongoing by first session"); }
        foreach (var route in new[] { "/api/memberships", "/api/invoices", "/api/members" })
            Check((await http.PostAsJsonAsync(route, new { })).StatusCode == HttpStatusCode.Forbidden, "B58 Manager cannot mutate cashier route " + route);
        Check((await http.PostAsync($"/api/memberships/{card.Id}/renew", null)).StatusCode == HttpStatusCode.Forbidden, "B58 Manager cannot renew");
        Check((await http.GetAsync($"/api/invoices/{paymentId}/export")).StatusCode == HttpStatusCode.Forbidden, "B58 Manager cannot print invoice");
        var unprofiledReception = Account(AccountRole.Receptionist, "reception_no_profile");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Accounts.Add(unprofiledReception); await db.SaveChangesAsync(); }
        Login(unprofiledReception);
        Check((await http.PostAsync($"/api/memberships/{card.Id}/renew?receptionistId={receptionist.Id}", null)).StatusCode == HttpStatusCode.BadRequest, "B58 unprofiled Receptionist cannot impersonate another collector");
        var counterClass = await NewClass("Counter course"); Login(receptionistAccount);
        var counterResult = await http.PostAsJsonAsync("/api/class-bookings", new { memberId = member.Id, classId = counterClass.Id, paymentMethod = 1 });
        Check(counterResult.IsSuccessStatusCode && (await Json(counterResult)).GetProperty("price").GetDecimal() == 100000, "B53 Receptionist collects class fee without membership");
        var counterInvoice = (await Json(counterResult)).GetProperty("invoiceId").GetGuid();
        using (var scope = app.Services.CreateScope()) { var row = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Invoices.FindAsync(counterInvoice);
            Check(row!.Status == InvoiceStatus.Paid && row.ReceptionistId == receptionist.Id, "B53 counter fee Paid and collector from token"); }
        Login(memberAccount);
        Check((await http.PostAsJsonAsync("/api/class-bookings", new { memberId = member.Id, classId = counterClass.Id })).StatusCode == HttpStatusCode.Forbidden, "B53 Member cannot use cashier bypass route");

        Login(cardAccount);
        var upgrade = await http.PostAsJsonAsync("/api/payments/upgrade-checkout", new { membershipId = card.Id, membershipPackageId = six.Id, paymentMethod = 2 });
        Check(upgrade.IsSuccessStatusCode, "B55 own valid paid card can upgrade"); var upgradeOrder = await Json(upgrade);
        Check(upgradeOrder.GetProperty("creditAmount").GetDecimal() == 533000 && upgradeOrder.GetProperty("amount").GetDecimal() == 3547000, "B55 credit uses actual paid amount and days AFTER today floored to 1000");
        Check((await http.PostAsJsonAsync("/api/payments/checkout", new { membershipPackageId = month.Id, startDate = today, paymentMethod = 2 })).StatusCode == HttpStatusCode.Conflict, "B55 upgrade blocks duplicate package checkout");
        Login(memberAccount);
        Check((await http.PostAsJsonAsync("/api/payments/upgrade-checkout", new { membershipId = card.Id, membershipPackageId = six.Id })).StatusCode == HttpStatusCode.NotFound, "B55 cannot upgrade another member card");
        Check((await Pay(upgradeOrder)).IsSuccessStatusCode, "B55 SePay settles upgrade");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var source = await db.Memberships.FindAsync(card.Id);
            var target = await db.Memberships.FindAsync(upgradeOrder.GetProperty("membershipId").GetGuid());
            Check(source!.Status == MembershipStatus.Expired && source.EndDate == today && target!.Status == MembershipStatus.Active && target.StartDate == today && target.UpgradedFromId == card.Id, "B55 payment replaces card atomically with origin link"); }
        Login(cardAccount);
        var downgrade = await http.PostAsJsonAsync("/api/payments/upgrade-checkout", new { membershipId = upgradeOrder.GetProperty("membershipId").GetGuid(), membershipPackageId = month.Id });
        Check(downgrade.StatusCode == HttpStatusCode.BadRequest, "B55 downgrade to shorter cheaper plan denied");
        var annual = new MembershipPackage { Id = Guid.NewGuid(), PackageName = "Gói 1 năm", Price = 6720000, DurationDays = 395, Status = MembershipStatus.Active, DiscountPercent = 10 };
        var future = new Membership { Id = Guid.NewGuid(), MemberId = cardMember.Id, PackageId = month.Id, StartDate = today.AddDays(187), EndDate = today.AddDays(216), Status = MembershipStatus.Active };
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.AddRange(annual, future); await db.SaveChangesAsync(); }
        Check((await http.PostAsJsonAsync("/api/payments/upgrade-checkout", new { membershipId = upgradeOrder.GetProperty("membershipId").GetGuid(), membershipPackageId = annual.Id })).StatusCode == HttpStatusCode.Conflict, "B55 prepaid future renewal blocks upgrade");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); (await db.Memberships.FindAsync(future.Id))!.IsDeleted = true; await db.SaveChangesAsync(); }
        var quote = await http.PostAsJsonAsync("/api/payments/upgrade-checkout", new { membershipId = upgradeOrder.GetProperty("membershipId").GetGuid(), membershipPackageId = annual.Id });
        Check(quote.IsSuccessStatusCode, "B55 new upgrade quote after removing future renewal"); var quoteOrder = await Json(quote);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); (await db.Memberships.FindAsync(future.Id))!.IsDeleted = false; await db.SaveChangesAsync(); }
        Check((await Pay(quoteOrder)).IsSuccessStatusCode, "B55 incoming transfer retained if source changed during hold");
        Check((await Json(await http.GetAsync("/api/payments/" + quoteOrder.GetProperty("paymentId").GetGuid()))).GetProperty("status").GetInt32() == 1, "B55 new future renewal prevents stale quote activation");
        await http.PostAsync("/api/payments/" + quoteOrder.GetProperty("paymentId").GetGuid() + "/cancel", null);
        var futureOnly = new Membership { Id = Guid.NewGuid(), MemberId = member.Id, PackageId = month.Id, StartDate = today.AddDays(1), EndDate = today.AddDays(30), Status = MembershipStatus.Active };
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(futureOnly); await db.SaveChangesAsync(); }
        var futureDiscountClass = await NewClass("Future card no discount"); var futureOrder = await ClassCheckout(futureDiscountClass, memberAccount);
        Check(futureOrder.GetProperty("discountPercent").GetInt32() == 0, "B53 discount checks payment day not future first session day");
        var pkg = await Json(await http.GetAsync($"/api/membership-packages/{month.Id}"));
        Check(pkg.GetProperty("discountPercent").GetInt32() == 10 && pkg.GetProperty("benefits").GetString()!.Contains("Tự tập"), "B53 packages expose benefits and discount");
        Login(manager);
        var notice = await http.PostAsJsonAsync("/api/notifications", new { accountIds = new[] { memberAccount.Id }, title = "Update", message = "New schedule", senderAccountId = coachAccount.Id });
        Check(notice.IsSuccessStatusCode && (await Json(notice)).GetProperty("senderAccountId").GetGuid() == manager.Id && (await Json(notice)).GetProperty("senderRole").GetString() == "CenterManager", "B57 sender from token not body");
        Login(memberAccount); var notices = await Json(await http.GetAsync("/api/notifications/me"));
        Check(notices.GetProperty("data").EnumerateArray().Any(n => n.GetProperty("senderName").GetString() == "manager"), "B57 notifications display sender");
        Login(manager);
        var room = new Room { Id = Guid.NewGuid(), RoomName = "Audit", Capacity = 20, Status = RoomStatus.Available };
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(room); await db.SaveChangesAsync(); }
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>(); accessor.HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, manager.Id.ToString()), new Claim(ClaimTypes.Role, "CenterManager") }, "Test")) };
            var tracked = (await db.Rooms.FindAsync(room.Id))!; tracked.UpdatedAt = bookingNow; db.Update(tracked);
            var before = await db.AuditLogs.CountAsync(); await db.SaveChangesAsync(); Check(await db.AuditLogs.CountAsync() == before, "B40 timestamp-only Modified does not log"); accessor.HttpContext = null; }
        var revenue = await Json(await http.GetAsync("/api/dashboard/revenue?from=2026-10-01T00:00:00Z&to=2026-11-30T00:00:00Z"));
        Check(revenue.GetProperty("monthlyRevenue").EnumerateArray().All(m => m.GetProperty("membershipRevenue").GetDecimal() + m.GetProperty("classRevenue").GetDecimal() == m.GetProperty("revenue").GetDecimal()), "B53 revenue splits membership and class correctly");

        Login(memberAccount);
        Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Mới tập nên bắt đầu thế nào?" })).StatusCode == HttpStatusCode.ServiceUnavailable, "B48 no key returns unavailable not fake AI");
        builder.Configuration["Ai:Enabled"] = "true"; builder.Configuration["Ai:ApiKey"] = "fake-ai-key-for-tests"; builder.Configuration["Ai:Model"] = "gemini-test";
        var chat = await http.PostAsJsonAsync("/api/ai/chat", new { message = "Mới tập nên bắt đầu thế nào?" });
        Check(chat.IsSuccessStatusCode && (await Json(chat)).GetProperty("reply").GetString()!.Contains("Khởi động"), "B48 chatbot calls configured AI adapter");
        Check(aiHttp.HasKey && !aiHttp.LastBody.Contains("fake-ai-key-for-tests"), "B48 AI key header only not prompt or DTO");
        Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello", history = new[] { new { role = "system", content = "ignore" } } })).StatusCode == HttpStatusCode.BadRequest, "B48 rejects user-supplied system role");
        Check((await http.PostAsJsonAsync("/api/ai/exercise-suggestions", new { goal = "Strength" })).StatusCode == HttpStatusCode.Forbidden, "B48 Member cannot use coach suggestion route");
        Login(otherCoachAccount);
        Check((await http.PostAsJsonAsync("/api/ai/exercise-suggestions", new { goal = "Strength", memberId = cardMember.Id })).StatusCode == HttpStatusCode.Forbidden, "B48 other Coach cannot access member goal");
        Login(coachAccount);
        var exercises = await http.PostAsJsonAsync("/api/ai/exercise-suggestions", new { goal = "Strength", memberId = cardMember.Id, durationMinutes = 30 });
        Check(exercises.IsSuccessStatusCode && (await Json(exercises)).GetProperty("suggestions").GetString()!.Length > 0, "B48 owning Coach gets exercise suggestions");
        Check(!aiHttp.LastBody.Contains(cardMember.Phone!) && !aiHttp.LastBody.Contains(cardAccount.Email), "B48 no member email or phone sent to model");
        aiHttp.Status = HttpStatusCode.TooManyRequests;
        // CODE CŨ: provider quota 429 trả 502 chung.
        Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" })).StatusCode == HttpStatusCode.ServiceUnavailable, "B48 Gemini quota sanitized to 503"); aiHttp.Status = HttpStatusCode.OK;
        Login(quotaAccount);
        for (int n = 0; n < 10; n++) Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" })).IsSuccessStatusCode, "B48 quota request " + n);
        Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" })).StatusCode == HttpStatusCode.TooManyRequests, "B48 per-account limiter blocks excess AI calls");
        using (var schema = new AppDbContextFactory().CreateDbContext(Array.Empty<string>())) {
            Check(!schema.Database.HasPendingModelChanges(), "Latest migration snapshot matches model");
            var script = schema.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().GenerateScript("20261029101049_AddSePayPayments");
            Check(script.Contains("CK_invoice_subject") && script.Contains("SenderAccountId") && script.Contains("RefundRequestedAt"), "Migration financial and sender SQL generated offline");
            Check(SWP_SportCenter.Service.Payment.PaymentQueries.Graph(schema).Where(i => i.Id == paymentId).ToQueryString().Contains("class_booking"), "PostgreSQL mixed invoice graph compiles");
        }
        // FIX B59–B65: regression cho các probe phát hiện ngày 30/10, không gọi Render.
        Login(receptionistAccount);
        Check((await http.PostAsync($"/api/payments/{futureOrder.GetProperty("paymentId").GetGuid()}/confirm", null)).StatusCode == HttpStatusCode.Conflict, "B60 staff cannot manually confirm SePay");
        Login(memberAccount);
        Check((await Json(await http.GetAsync($"/api/payments/{futureOrder.GetProperty("paymentId").GetGuid()}"))).GetProperty("status").GetInt32() == 1, "B60 rejected confirm leaves payment Pending");

        var draft = new Invoice { Id = Guid.NewGuid(), MemberMembershipId = futureOnly.Id, Kind = "Membership", Amount = 1000, Status = InvoiceStatus.Pending, PaymentMethod = PaymentMethod.Cash };
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(draft); await db.SaveChangesAsync(); }
        Login(receptionistAccount);
        Check((await http.DeleteAsync($"/api/invoices/{draft.Id}")).IsSuccessStatusCode, "B65 pending counter draft deleted");
        Check((await http.GetAsync($"/api/invoices/{draft.Id}")).StatusCode == HttpStatusCode.NotFound, "B65 deleted invoice detail 404");
        Check(!(await http.GetStringAsync("/api/invoices")).Contains(draft.Id.ToString()), "B65 deleted invoice absent from active list");
        Check((await http.DeleteAsync($"/api/invoices/{draft.Id}")).StatusCode == HttpStatusCode.NotFound, "B65 repeat delete not false success");
        Login(memberAccount);
        Check(!(await http.GetStringAsync("/api/members/me/invoices")).Contains(draft.Id.ToString()), "B65 deleted invoice absent from own history");

        var expiredCard = new Membership { Id = Guid.NewGuid(), MemberId = quotaMember.Id, PackageId = month.Id, StartDate = today, EndDate = today.AddDays(29), Status = MembershipStatus.Inactive };
        var expiredInvoice = new Invoice { Id = Guid.NewGuid(), Membership = expiredCard, Amount = 800000, Kind = "Membership", Status = InvoiceStatus.Pending, PaymentMethod = PaymentMethod.BankTransfer, PaymentExpiresAt = bookingNow.AddSeconds(-1), PaymentReference = "SWP" + Guid.NewGuid().ToString("N") };
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(expiredInvoice); await db.SaveChangesAsync(); }
        Login(quotaAccount);
        Check((await Json(await http.GetAsync($"/api/payments/{expiredInvoice.Id}"))).GetProperty("status").GetInt32() == 5, "B59 read returns Expired");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Check((await db.Invoices.FindAsync(expiredInvoice.Id))!.Status == InvoiceStatus.Cancelled && (await db.Memberships.FindAsync(expiredCard.Id))!.Status == MembershipStatus.Cancelled, "B59 payment read persists invoice and membership expiration together"); }

        Login(coachAccount);
        Check((await http.PostAsJsonAsync("/api/training-plans", new { coachId = coach.Id, classId = counterClass.Id, memberId = cardMember.Id, title = "Invalid student", createdDate = today })).StatusCode == HttpStatusCode.BadRequest, "B62 single plan rejects unbooked member");
        Login(otherCoachAccount);
        Check((await http.PostAsJsonAsync("/api/training-plans", new { coachId = otherCoach.Id, classId = counterClass.Id, memberId = member.Id, title = "Wrong class", createdDate = today })).StatusCode == HttpStatusCode.Forbidden, "B62 single plan rejects other coach class");
        Login(coachAccount);
        var validPlan = await http.PostAsJsonAsync("/api/training-plans", new { coachId = coach.Id, classId = counterClass.Id, memberId = member.Id, title = "Valid student", createdDate = today });
        Check(validPlan.IsSuccessStatusCode, "B62 valid single plan still works");
        var planId = (await Json(validPlan)).GetProperty("trainingPlanId").GetGuid();
        var resultRecord = new TrainingResult { Id = Guid.NewGuid(), PlanId = planId, MemberId = member.Id, CoachId = coach.Id, PerformanceScore = 8, EvaluationDate = today };
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(resultRecord); await db.SaveChangesAsync(); }
        Check((await http.GetAsync($"/api/training-plans/{planId}")).IsSuccessStatusCode && (await http.GetAsync($"/api/training-results/{resultRecord.Id}")).IsSuccessStatusCode, "B61 owning coach reads both details");
        Login(otherCoachAccount);
        Check((await http.GetAsync($"/api/training-plans/{planId}")).StatusCode == HttpStatusCode.NotFound, "B61 other coach plan detail 404");
        Check((await http.GetAsync($"/api/training-results/{resultRecord.Id}")).StatusCode == HttpStatusCode.NotFound, "B61 other coach result detail 404");
        Login(receptionistAccount);
        using (var multipart = new MultipartFormDataContent()) {
            var bytes = new ByteArrayContent(new byte[6 * 1024 * 1024]); bytes.Headers.ContentType = new("image/png"); multipart.Add(bytes, "file", "large.png");
            var rejected = await http.PostAsync("/api/media/images", multipart);
            Check(rejected.StatusCode == HttpStatusCode.RequestEntityTooLarge && rejected.Content.Headers.ContentType?.MediaType?.Contains("json") == true, "B64 6 MiB image rejected as JSON 413 before provider");
        }
        // FIX SEPAY: mã ngân hàng chèn space, retry receipt cũ và không bypass đối soát.
        const string canonical = "SWP8D0D7C0313844457855404F957796EEB";
        var parsed = SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract("SWP8D0D7C0313844457855404F957796E EB CKN 377757");
        Check(parsed.SequenceEqual(new[] { canonical }), "SePay real bank split reference reconstructed exactly");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract(canonical.ToLowerInvariant()).Single() == canonical, "SePay lowercase canonical normalized");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract(canonical, canonical.Insert(20, "\t\r\n")).Length == 1, "SePay whitespace variants deduplicated");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract(canonical[..^2] + " CKN 377757").Length == 0, "SePay missing hex not guessed");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract(canonical + "F").Length == 0, "SePay extra hex suffix rejected");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract("X" + canonical).Length == 0, "SePay reference embedded in larger word rejected");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract(canonical[..^2], canonical[^2..]).Length == 0, "SePay does not stitch Code and Content together");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract(canonical.Insert(20, "-")).Length == 0, "SePay punctuation inside code not normalized");
        Check(SWP_SportCenter.Service.Sepay.PaymentReferenceParser.Extract(canonical, "SWP" + new string('A', 32)).Length == 2, "SePay conflicting complete references remain ambiguous");

        SWP_SportCenter.Service.Sepay.Request.SepayWebhookDto BankPayload(JsonElement purchase) {
            var reference = purchase.GetProperty("paymentReference").GetString()!;
            return new() { Id = ++eventId, Content = reference[..33] + " " + reference[33..] + " CKN 377757 - BANK IBFT",
                TransferAmount = purchase.GetProperty("amount").GetDecimal(), ReferenceCode = "BANK" + eventId,
                TransactionDate = "2026-10-29 10:00:00", Gateway = "MBBank", TransferType = "in", AccountNumber = "000000000000" };
        }
        async Task<HttpResponseMessage> SendBank(SWP_SportCenter.Service.Sepay.Request.SepayWebhookDto body, bool authenticated = true) {
            using var client = new HttpClient { BaseAddress = http.BaseAddress };
            if (authenticated) client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Apikey test-only-sepay-webhook-key-not-a-production-secret-2026");
            return await client.PostAsJsonAsync("/api/sepay/webhook", body);
        }
        async Task SeedLegacy(SWP_SportCenter.Service.Sepay.Request.SepayWebhookDto body) {
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body))));
            using var scope = app.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Set<SePayWebhookReceipt>().Add(new() { Id = body.Id, Content = body.Content, TransferAmount = body.TransferAmount,
                AccountNumber = body.AccountNumber, Gateway = body.Gateway, BankReferenceCode = body.ReferenceCode,
                BankTransactionDate = body.TransactionDate, PayloadHash = hash, ReceivedAt = bookingNow.AddMinutes(-1), Outcome = "Unmatched" });
            await db.SaveChangesAsync();
        }
        var spacedOrder = await ClassCheckout(await NewClass("Split reference"), memberAccount); var spacedBody = BankPayload(spacedOrder);
        Check((await SendBank(spacedBody, false)).StatusCode == HttpStatusCode.Unauthorized, "SePay split reference still requires webhook API key");
        Check((await SendBank(spacedBody)).IsSuccessStatusCode, "SePay split reference webhook acknowledged");
        var settled = await Json(await http.GetAsync($"/api/payments/{spacedOrder.GetProperty("paymentId").GetGuid()}"));
        Check(settled.GetProperty("status").GetInt32() == 2 && settled.GetProperty("qrCodeUrl").ValueKind == JsonValueKind.Null, "SePay split reference sets Paid and removes QR from status response");
        using (var scope = app.Services.CreateScope()) { var row = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<SePayWebhookReceipt>().FindAsync(spacedBody.Id);
            Check(row!.InvoiceId == spacedOrder.GetProperty("paymentId").GetGuid() && row.Outcome == "Paid", "SePay split reference saves InvoiceId on receipt"); }
        Check((await SendBank(spacedBody)).IsSuccessStatusCode, "SePay Paid retry remains idempotent");
        Check((await Json(await http.GetAsync($"/api/payments/{spacedOrder.GetProperty("paymentId").GetGuid()}"))).GetProperty("paidAt").GetString() == settled.GetProperty("paidAt").GetString(), "SePay retry does not change payment date");
        spacedBody.TransferAmount += 1;
        Check((await SendBank(spacedBody)).StatusCode == HttpStatusCode.Conflict, "SePay same event changed payload rejected");

        var legacyOrder = await ClassCheckout(await NewClass("Legacy unmatched retry"), memberAccount); var legacyBody = BankPayload(legacyOrder);
        await SeedLegacy(legacyBody);
        Check((await SendBank(legacyBody)).IsSuccessStatusCode, "SePay identical legacy Unmatched retry accepted");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var row = await db.Set<SePayWebhookReceipt>().FindAsync(legacyBody.Id);
            Check(row!.InvoiceId == legacyOrder.GetProperty("paymentId").GetGuid() && row.Outcome == "Paid" && row.ReceivedAt == bookingNow.AddMinutes(-1), "SePay retry updates same receipt preserves original receive time");
            Check(await db.Set<SePayWebhookReceipt>().CountAsync(x => x.Id == legacyBody.Id) == 1, "SePay retry creates no duplicate receipt"); }

        var lateOrder = await ClassCheckout(await NewClass("Expired unmatched retry"), memberAccount); var lateBody = BankPayload(lateOrder); await SeedLegacy(lateBody);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var row = (await db.Invoices.FindAsync(lateOrder.GetProperty("paymentId").GetGuid()))!;
            row.PaymentExpiresAt = bookingNow.AddSeconds(-1); await db.SaveChangesAsync(); }
        await SendBank(lateBody);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); var row = await db.Set<SePayWebhookReceipt>().FindAsync(lateBody.Id);
            Check(row!.InvoiceId == lateOrder.GetProperty("paymentId").GetGuid() && row.Outcome == "RejectedState" && (await db.Invoices.FindAsync(row.InvoiceId))!.Status != InvoiceStatus.Paid, "SePay legacy retry links invoice but never resurrects expired payment"); }

        var mismatchOrder = await ClassCheckout(await NewClass("Split wrong amount"), memberAccount); var mismatchBody = BankPayload(mismatchOrder); mismatchBody.TransferAmount -= 1;
        await SendBank(mismatchBody);
        using (var scope = app.Services.CreateScope()) { var row = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<SePayWebhookReceipt>().FindAsync(mismatchBody.Id);
            Check(row!.InvoiceId == mismatchOrder.GetProperty("paymentId").GetGuid() && row.Outcome == "AmountMismatch", "SePay split reference still enforces exact amount"); }
        var wrongBankOrder = await ClassCheckout(await NewClass("Split wrong receiver"), memberAccount); var wrongBankBody = BankPayload(wrongBankOrder); wrongBankBody.AccountNumber = "WRONG_ACCOUNT";
        await SendBank(wrongBankBody);
        using (var scope = app.Services.CreateScope()) { var row = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<SePayWebhookReceipt>().FindAsync(wrongBankBody.Id);
            Check(row!.Outcome == "WrongReceiver", "SePay split reference still enforces receiver"); }
        var ambiguousBody = BankPayload(wrongBankOrder); ambiguousBody.Code = canonical;
        await SendBank(ambiguousBody);
        using (var scope = app.Services.CreateScope()) { var row = await scope.ServiceProvider.GetRequiredService<AppDbContext>().Set<SePayWebhookReceipt>().FindAsync(ambiguousBody.Id);
            Check(row!.Outcome == "AmbiguousReference" && row.InvoiceId == null, "SePay conflicting fields cannot mark an arbitrary invoice Paid"); }
        // FIX B48 01/11: contract Gemini, context live và không cần key thật.
        var swaggerAI = await Json(await http.GetAsync("/swagger/v1/swagger.json"));
        var chatResponses = swaggerAI.GetProperty("paths").GetProperty("/api/ai/chat").GetProperty("post").GetProperty("responses");
        Check(chatResponses.GetProperty("200").GetProperty("content").GetProperty("application/json").GetProperty("schema").GetProperty("$ref").GetString()!.Contains("AiReply"), "B48 Swagger has typed Gemini reply schema");
        Check(chatResponses.TryGetProperty("503", out _) && chatResponses.TryGetProperty("429", out _), "B48 Swagger documents unavailable and rate limit");
        http.DefaultRequestHeaders.Authorization = null;
        Check((await http.GetAsync("/api/ai/options")).StatusCode == HttpStatusCode.Unauthorized, "B48 config metadata requires login");
        Login(manager);
        var ready = await Json(await http.GetAsync("/api/ai/options"));
        Check(ready.GetProperty("provider").GetString() == "Gemini" && ready.GetProperty("configured").GetBoolean() && !ready.GetProperty("providerVerified").GetBoolean(), "B48 configured metadata does not claim provider health");
        Check(!ready.ToString().Contains("fake-ai-key") && !ready.ToString().Contains("gemini-test"), "B48 config metadata exposes no key or model value");
        var cfg = new AiOptions { Enabled = true, ApiKey = "local-placeholder", Model = "INVALID" };
        Check(cfg.ConfigurationIssues().SequenceEqual(new[] { "Ai__Model" }), "B48 reports bad model setting without secret");
        builder.Configuration["Ai:Enabled"] = "false";
        var disabled = await Json(await http.GetAsync("/api/ai/options"));
        Check(!disabled.GetProperty("configured").GetBoolean() && disabled.GetProperty("configurationIssues")[0].GetString() == "Ai__Enabled", "B48 missing config metadata distinguishes disabled");
        builder.Configuration["Ai:Enabled"] = "true";
        foreach (var staffAccount in new[] { manager, receptionistAccount, coachAccount }) {
            Login(staffAccount);
            Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Các môn hiện có?" })).IsSuccessStatusCode, "B48 authenticated staff chat allowed " + staffAccount.Role);
        }
        var aiMemberAccount = Account(AccountRole.Member, "ai_context_owner");
        var aiMember = new Member { Id = Guid.NewGuid(), Account = aiMemberAccount, FullName = "AI context owner", Phone = "0900000099" };
        var ownClass = await NewClass("AI_PRIVATE_OWN_SCHEDULE"); var foreignClass = await NewClass("AI_OTHER_MEMBER_SCHEDULE");
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); db.Add(aiMember);
            db.ClassBookings.AddRange(new ClassBooking { Id = Guid.NewGuid(), MemberId = aiMember.Id, ClassId = ownClass.Id, Status = BookingStatus.Booked },
                new ClassBooking { Id = Guid.NewGuid(), MemberId = cardMember.Id, ClassId = foreignClass.Id, Status = BookingStatus.Booked }); await db.SaveChangesAsync(); }
        Login(aiMemberAccount);
        var contextual = await http.PostAsJsonAsync("/api/ai/chat", new { message = "Tôi học khi nào?", memberId = cardMember.Id,
            history = new[] { new { role = "user", content = "Các môn?" }, new { role = "assistant", content = "Để mình kiểm tra." } } });
        Check(contextual.IsSuccessStatusCode, "B48 Gemini chat supports bounded history and live context");
        using (var requestJson = JsonDocument.Parse(aiHttp.LastBody)) {
            var system = requestJson.RootElement.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString()!;
            var marker = " Dữ liệu hiện tại: "; using var ctx = JsonDocument.Parse(system[(system.LastIndexOf(marker, StringComparison.Ordinal) + marker.Length)..]);
            Check(ctx.RootElement.GetProperty("Categories").EnumerateArray().Any(v => v.GetString() == "Gym"), "B48 sports catalog injected from current DB");
            var schedule = ctx.RootElement.GetProperty("PersonalSchedule").EnumerateArray().Select(v => v.GetProperty("ClassName").GetString()).ToArray();
            Check(schedule.Contains(ownClass.ClassName) && !schedule.Contains(foreignClass.ClassName), "B48 personal schedule uses token not client memberId");
            Check(requestJson.RootElement.GetProperty("contents")[1].GetProperty("role").GetString() == "model", "B48 assistant history maps to Gemini model role");
        }
        Check(!aiHttp.LastBody.Contains(aiMember.Phone!) && !aiHttp.LastBody.Contains(aiMemberAccount.Email), "B48 chat context sends no account email or phone");
        var normalBody = aiHttp.Body;
        aiHttp.Body = "{\"candidates\":{}}";
        var malformed = await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" });
        Check(malformed.StatusCode == HttpStatusCode.BadGateway && (await Json(malformed)).TryGetProperty("message", out _), "B48 malformed provider schema gives sanitized JSON 502 not 500");
        aiHttp.Body = normalBody; aiHttp.Status = HttpStatusCode.Forbidden;
        var rejectedKey = await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" });
        Check(rejectedKey.StatusCode == HttpStatusCode.ServiceUnavailable && !(await rejectedKey.Content.ReadAsStringAsync()).Contains("fake-ai-key"), "B48 Gemini permission error gives safe 503");
        aiHttp.Status = HttpStatusCode.OK;
        Login(quotaAccount);
        var limitedJson = await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" });
        Check(limitedJson.StatusCode == HttpStatusCode.TooManyRequests && (await Json(limitedJson)).GetProperty("status").GetInt32() == 429, "B48 local quota has stable problem JSON");
        // FIX B48 02/11: chỉ dùng key giả, không đưa key người dùng đã lộ vào test/Git.
        const string opaqueKey = "AQ.synthetic-only.key-format-test_+/=";
        Check(AiOptions.IsSafeApiKey(opaqueKey), "B48 dotted opaque key accepted without guessing Google format");
        foreach (var invalidKey in new string?[] { null, "", " ", " key", "key ", "key\r\nInjected: yes", "key\t", "key\0", "key\u007f", "key\u00e9", new string('a',4097) })
            Check(!AiOptions.IsSafeApiKey(invalidKey), "B48 unsafe empty whitespace control or oversized key rejected");
        builder.Configuration["Ai:ApiKey"] = opaqueKey;
        Login(aiMemberAccount);
        var dottedOptions = await Json(await http.GetAsync("/api/ai/options"));
        Check(dottedOptions.GetProperty("configured").GetBoolean() && !dottedOptions.ToString().Contains(opaqueKey), "B48 dotted key configured metadata safe");
        Check((await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" })).IsSuccessStatusCode && aiHttp.LastApiKey == opaqueKey, "B48 dotted key reaches Gemini header unchanged");
        builder.Configuration["Ai:ApiKey"] = "bad\r\nsecret";
        var invalidHeaderKey = await http.PostAsJsonAsync("/api/ai/chat", new { message = "Hello" });
        Check(invalidHeaderKey.StatusCode == HttpStatusCode.ServiceUnavailable && !(await invalidHeaderKey.Content.ReadAsStringAsync()).Contains("secret"), "B48 invalid header key fails safely before provider");
        builder.Configuration["Ai:ApiKey"] = "fake-ai-key-for-tests";
        Login(memberAccount);
        var timing = await http.GetAsync($"/api/payments/{spacedOrder.GetProperty("paymentId").GetGuid()}");
        Check(timing.Headers.TryGetValues("Server-Timing", out var headers) && headers.Single().StartsWith("payment;dur="), "Payment polling exposes backend timing without claiming bank latency");
        // FIX B58 02/11: quy tắc đã chốt chỉ xóa trước buổi đầu, cả khi lớp đã kết thúc/hủy.
        Login(manager);
        foreach (var status in new[] { ClassStatus.Scheduled, ClassStatus.Ongoing, ClassStatus.Completed, ClassStatus.Cancelled }) {
            var pastClass = await NewClass("B58 ended session " + status, 0);
            using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                (await db.Classes.FindAsync(pastClass.Id))!.Status = status;
                var session = await db.ClassSessions.SingleAsync(s => s.ClassId == pastClass.Id);
                session.Date = DateOnly.FromDateTime(today).AddDays(-2); session.StartTime = TimeSpan.FromHours(6.25); session.EndTime = TimeSpan.FromHours(6.5);
                await db.SaveChangesAsync(); }
            var denied = await http.DeleteAsync($"/api/classes/{pastClass.Id}");
            Check(denied.StatusCode == HttpStatusCode.BadRequest && (await Json(denied)).GetProperty("message").GetString()!.Contains("Chỉ được xóa"), "B58 rejects ended first session and clear JSON for " + status);
            Check((await http.GetAsync($"/api/classes/{pastClass.Id}")).IsSuccessStatusCode, "B58 rejected delete keeps class visible " + status);
        }
        var boundaryClass = await NewClass("B58 exact first start", 0);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.ClassSessions.SingleAsync(s => s.ClassId == boundaryClass.Id);
            session.Date = DateOnly.FromDateTime(today); session.StartTime = TimeSpan.FromHours(10); session.EndTime = TimeSpan.FromHours(11);
            await db.SaveChangesAsync(); }
        Check((await http.DeleteAsync($"/api/classes/{boundaryClass.Id}")).StatusCode == HttpStatusCode.BadRequest, "B58 exact 10h Vietnam 03h UTC blocks deletion");
        var historicalClass = await NewClass("B58 soft deleted past session", 0);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var session = await db.ClassSessions.SingleAsync(s => s.ClassId == historicalClass.Id); session.Date = DateOnly.FromDateTime(today).AddDays(-1); session.IsDeleted = true; await db.SaveChangesAsync(); }
        Check((await http.DeleteAsync($"/api/classes/{historicalClass.Id}")).StatusCode == HttpStatusCode.BadRequest, "B58 hiding past session cannot bypass deletion guard");
        var futureCancelled = await NewClass("B58 cancelled before first session", 0);
        using (var scope = app.Services.CreateScope()) { var db = scope.ServiceProvider.GetRequiredService<AppDbContext>(); (await db.Classes.FindAsync(futureCancelled.Id))!.Status = ClassStatus.Cancelled; await db.SaveChangesAsync(); }
        Check((await http.DeleteAsync($"/api/classes/{futureCancelled.Id}")).IsSuccessStatusCode, "B58 cancelled before any first start can soft-delete");
        var futureScheduled = await NewClass("B58 scheduled before first session", 0);
        Check((await http.DeleteAsync($"/api/classes/{futureScheduled.Id}")).IsSuccessStatusCode, "B58 future Scheduled class remains deletable");
        // FIX Gemini 3.8: transport tests dùng key/provider giả, không dùng quota thật.
        var geminiOptions = new AiOptions { Enabled = true, ApiKey = "synthetic.secret-key.with-dot", Model = "gemini-3.8-flash", TotalTimeoutSeconds = 10, AttemptTimeoutSeconds = 5 };
        var gen = GeminiClient.GenerationConfig(geminiOptions);
        Check(!gen.ContainsKey("temperature") && !gen.ContainsKey("topP") && !gen.ContainsKey("topK"), "Gemini 3.8 omits deprecated sampling params");
        using (var configDoc = JsonDocument.Parse(JsonSerializer.Serialize(gen)))
            Check(configDoc.RootElement.GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString() == "LOW" && configDoc.RootElement.GetProperty("maxOutputTokens").GetInt32() == 4096, "Gemini 3.8 LOW thinking with bounded non-tiny output");
        Check(!GeminiClient.GenerationConfig(new AiOptions { Model = "gemini-2.5-flash" }).ContainsKey("thinkingConfig"), "Gemini 2.x not sent incompatible thinkingLevel");
        Check(new AiOptions { Enabled = true, ApiKey = "safe", Model = "gemini-3.8-flash", ThinkingLevel = "MINIMAL" }.ConfigurationIssues().Contains("Ai__ThinkingLevel"), "Gemini 3.8 MINIMAL rejected locally");
        Check(new AiOptions { Enabled = true, ApiKey = "safe", Model = "gemini-test", MaxRetries = 100 }.ConfigurationIssues().Contains("Ai__MaxRetries"), "Gemini unbounded retry configuration rejected");
        var geminiLog = new GeminiTestLogger();
        const string syntheticReply = "{\"candidates\":[{\"finishReason\":\"STOP\",\"content\":{\"parts\":[{\"thought\":true,\"text\":\"hidden-thought\"},{\"text\":\"Real synthetic provider reply\"}]}}]}";
        async Task<(string? Reply, Exception? Error)> InvokeGemini(GeminiScriptHandler handler, CancellationToken token = default) {
            using var transport = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
            var provider = new GeminiClient(transport, new GeminiTestOptions(geminiOptions), geminiLog, new HttpContextAccessor());
            try { return (await provider.GenerateAsync("private-system-context", new[] { new ChatMessage { Content = "private-user-prompt" } }, token), null); }
            catch (Exception error) { return (null, error); }
        }
        var transientHandler = new GeminiScriptHandler();
        transientHandler.Responses.Enqueue((503, "{\"error\":{\"status\":\"UNAVAILABLE\",\"message\":\"private-google-body\"}}", null));
        transientHandler.Responses.Enqueue((200, syntheticReply, null));
        var transientReply = await InvokeGemini(transientHandler);
        Check(transientReply.Reply == "Real synthetic provider reply" && transientHandler.Calls == 2, "Gemini transient 503 retries once then returns provider text only");
        using (var sent = JsonDocument.Parse(transientHandler.RequestBodies[0]))
            Check(sent.RootElement.GetProperty("generationConfig").GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString() == "LOW", "Gemini HTTP body contains model-specific thinking config");
        Check(geminiLog.Messages.Any(m => m.Contains("httpStatus 503") && m.Contains("UNAVAILABLE")) &&
            !string.Join(" ", geminiLog.Messages).Contains("private-google-body") && !string.Join(" ", geminiLog.Messages).Contains(geminiOptions.ApiKey) &&
            !string.Join(" ", geminiLog.Messages).Contains("private-user-prompt") && !string.Join(" ", geminiLog.Messages).Contains("private-system-context"), "Gemini diagnostic logs contain status not key prompt or raw Google body");
        var permanentHandler = new GeminiScriptHandler(); permanentHandler.Responses.Enqueue((403, "{\"error\":{\"status\":\"PERMISSION_DENIED\"}}", null));
        var permanent = await InvokeGemini(permanentHandler);
        Check(permanent.Error is AiUnavailableException && permanentHandler.Calls == 1, "Gemini auth permission error never retried");
        var dailyHandler = new GeminiScriptHandler(); dailyHandler.Responses.Enqueue((429, "{\"error\":{\"status\":\"RESOURCE_EXHAUSTED\",\"details\":[{\"violations\":[{\"quotaId\":\"GenerateRequestsPerDayPerProject\"}]},{\"retryDelay\":\"0.01s\"}]}}", null));
        var dailyReply = await InvokeGemini(dailyHandler);
        Check(dailyReply.Error is AiUnavailableException && dailyReply.Error.Message.Contains("hạn mức ngày") && dailyHandler.Calls == 1, "Gemini daily project quota not blindly retried");
        var unknownQuota = new GeminiScriptHandler(); unknownQuota.Responses.Enqueue((429, "{\"error\":{\"status\":\"RESOURCE_EXHAUSTED\"}}", null));
        Check((await InvokeGemini(unknownQuota)).Error is AiUnavailableException && unknownQuota.Calls == 1, "Gemini unknown quota without RetryInfo not guessed transient");
        var minuteQuota = new GeminiScriptHandler(); minuteQuota.Responses.Enqueue((429, "{\"error\":{\"status\":\"RESOURCE_EXHAUSTED\",\"details\":[{\"retryDelay\":\"0.01s\"}]}}", null)); minuteQuota.Responses.Enqueue((200, syntheticReply, null));
        Check((await InvokeGemini(minuteQuota)).Reply != null && minuteQuota.Calls == 2, "Gemini short transient RetryInfo permits one bounded retry");
        var headerQuota = new GeminiScriptHandler(); headerQuota.Responses.Enqueue((429, "{}", TimeSpan.FromSeconds(1))); headerQuota.Responses.Enqueue((200, syntheticReply, null));
        Check((await InvokeGemini(headerQuota)).Reply != null && headerQuota.Calls == 2, "Gemini short Retry-After header alone permits bounded retry");
        var longRetry = new GeminiScriptHandler(); longRetry.Responses.Enqueue((503, "{}", TimeSpan.FromSeconds(30)));
        Check((await InvokeGemini(longRetry)).Error is AiUnavailableException && longRetry.Calls == 1, "Gemini does not ignore long Retry-After or exceed budget waiting");
        var exhausted = new GeminiScriptHandler(); exhausted.Responses.Enqueue((503,"{}", null)); exhausted.Responses.Enqueue((503,"{}", null));
        Check((await InvokeGemini(exhausted)).Error is AiUnavailableException && exhausted.Calls == 2, "Gemini retry count capped after repeated 503");
        var cappedOutput = new GeminiScriptHandler(); cappedOutput.Responses.Enqueue((200, "{\"candidates\":[{\"finishReason\":\"MAX_TOKENS\",\"content\":{\"parts\":[{\"text\":\"unfinished\"}]}}]}", null));
        Check((await InvokeGemini(cappedOutput)).Error is AiUnavailableException && cappedOutput.Calls == 1, "Gemini MAX_TOKENS not fake completed reply or retried");
        var emptyOutput = new GeminiScriptHandler(); emptyOutput.Responses.Enqueue((200, "{\"candidates\":[{\"content\":{\"parts\":[]}}]}", null));
        Check((await InvokeGemini(emptyOutput)).Error is AiProviderException, "Gemini empty output not fake 200 answer");
        var callerCancelled = new GeminiScriptHandler(); using (var cancellation = new CancellationTokenSource()) {
            cancellation.Cancel(); var cancelledReply = await InvokeGemini(callerCancelled, cancellation.Token);
            Check(cancelledReply.Error is OperationCanceledException && callerCancelled.Calls == 0, "Gemini caller cancellation respected before outbound call"); }
        var blockedNetwork = new GeminiScriptHandler { WaitForCancellation = true }; var timeoutTimer = System.Diagnostics.Stopwatch.StartNew();
        Check((await InvokeGemini(blockedNetwork)).Error is AiUnavailableException && blockedNetwork.Calls == 1 && timeoutTimer.Elapsed < TimeSpan.FromSeconds(9), "Gemini attempt timeout bounded and not blindly replayed");
        Console.WriteLine($"RESULT {passed} workflow checks passed"); await app.StopAsync(); await app.DisposeAsync();
    }
}

internal sealed class AiStubHandler : HttpMessageHandler
{
    public string? LastApiKey; // FIX: provider giả giữ key giả kiểm tra header, không log.
    public string LastBody = "";
    public bool HasKey;
    public int Calls;
    public HttpStatusCode Status = HttpStatusCode.OK;
    public string Body = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Khởi động nhẹ, tập đúng kỹ thuật và dừng nếu đau.\"}]}}]}";
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Calls++; HasKey = request.Headers.Contains("x-goog-api-key"); LastBody = await request.Content!.ReadAsStringAsync(ct);
        LastApiKey = HasKey ? request.Headers.GetValues("x-goog-api-key").Single() : null;
        if (request.RequestUri!.Host != "generativelanguage.googleapis.com") throw new Exception("Unexpected AI endpoint");
        return new HttpResponseMessage(Status) { Content = new StringContent(Body, System.Text.Encoding.UTF8, "application/json") };
    }
}

internal sealed class GeminiTestOptions(AiOptions value) : IOptionsSnapshot<AiOptions>
{
    public AiOptions Value => value;
    public AiOptions Get(string? name) => value;
}
internal sealed class GeminiTestLogger : ILogger<GeminiClient>
{
    public readonly List<string> Messages = new();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Messages.Add(formatter(state, exception));
}
internal sealed class GeminiScriptHandler : HttpMessageHandler
{
    public readonly Queue<(int Status, string Body, TimeSpan? Retry)> Responses = new();
    public readonly List<string> RequestBodies = new();
    public int Calls;
    public bool WaitForCancellation;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Calls++; RequestBodies.Add(await request.Content!.ReadAsStringAsync(ct));
        if (WaitForCancellation) await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        var next = Responses.Dequeue(); var response = new HttpResponseMessage((HttpStatusCode)next.Status) { Content = new StringContent(next.Body, System.Text.Encoding.UTF8, "application/json") };
        if (next.Retry.HasValue) response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(next.Retry.Value);
        return response;
    }
}
