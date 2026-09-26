using Microsoft.EntityFrameworkCore;
using SWP_SportCenter.Repository.Entity;

namespace SWP_SportCenter.Repository;

public class AppDbContext: DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Account
    public DbSet<Account> Accounts { get; set; }

    // Notification
    public DbSet<Notification> Notifications { get; set; }

    // Member
    public DbSet<Member> Members { get; set; }

    // Coach
    public DbSet<Coach> Coaches { get; set; }

    // Receptionist
    public DbSet<Receptionist> Receptionists { get; set; }

    // Center Manager
    public DbSet<CenterManager> CenterManagers { get; set; }

    // Membership
    public DbSet<MembershipPackage> MembershipPackages { get; set; }

    public DbSet<Membership> Memberships { get; set; }

    // Invoice
    public DbSet<Invoice> Invoices { get; set; }

    // Sport
    public DbSet<SportCategory> SportCategories { get; set; }

    public DbSet<Class> Classes { get; set; }

    public DbSet<ClassSession> ClassSessions { get; set; }

    public DbSet<ClassBooking> ClassBookings { get; set; }

    public DbSet<Attendance> Attendances { get; set; }

    // Training
    public DbSet<TrainingPlan> TrainingPlans { get; set; }

    public DbSet<TrainingResult> TrainingResults { get; set; }

    // Other
    public DbSet<AuditLog> AuditLogs { get; set; }

    public DbSet<Room> Rooms { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);


        // =========================================================
        // ACCOUNT
        // =========================================================

        modelBuilder.Entity<Account>(builder =>
        {
            builder.ToTable("account");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("account_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.Username)
                .HasColumnName("username")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Password)
                .HasColumnName("password")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.Role)
                .HasColumnName("role")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at");

            // Username không được trùng
            builder.HasIndex(x => x.Username)
                .IsUnique();

            builder.HasIndex(x => x.Role);

            builder.HasIndex(x => x.Status);
        });


        // =========================================================
        // NOTIFICATION
        // =========================================================

        modelBuilder.Entity<Notification>(builder =>
        {
            builder.ToTable("notification");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("notification_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Message)
                .HasColumnName("message")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(x => x.IsRead)
                .HasColumnName("is_read")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.HasIndex(x => x.AccountId);

            builder.HasIndex(x => x.IsRead);

            builder.HasOne(x => x.Account)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // MEMBER
        // =========================================================

        modelBuilder.Entity<Member>(builder =>
        {
            builder.ToTable("member");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("member_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(x => x.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Dob)
                .HasColumnName("dob")
                .IsRequired();

            builder.Property(x => x.Gender)
                .HasColumnName("gender")
                .HasMaxLength(20);

            builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Avatar)
                .HasColumnName("avatar")
                .HasMaxLength(500);

            builder.Property(x => x.TrainingGoal)
                .HasColumnName("training_goal")
                .HasMaxLength(500);

            // Một Account chỉ có một Member
            builder.HasIndex(x => x.AccountId)
                .IsUnique();

            builder.HasIndex(x => x.Phone)
                .IsUnique();

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.HasOne(x => x.Account)
                .WithOne(x => x.Member)
                .HasForeignKey<Member>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // COACH
        // =========================================================

        modelBuilder.Entity<Coach>(builder =>
        {
            builder.ToTable("coach");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("coach_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(x => x.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Avatar)
                .HasColumnName("avatar")
                .HasMaxLength(500);

            builder.Property(x => x.Specialization)
                .HasColumnName("specialization")
                .HasMaxLength(200);

            builder.Property(x => x.ExperienceYears)
                .HasColumnName("experience_years")
                .IsRequired();

            builder.HasIndex(x => x.AccountId)
                .IsUnique();

            builder.HasIndex(x => x.Phone)
                .IsUnique();

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.HasOne(x => x.Account)
                .WithOne(x => x.Coach)
                .HasForeignKey<Coach>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // RECEPTIONIST
        // =========================================================

        modelBuilder.Entity<Receptionist>(builder =>
        {
            builder.ToTable("receptionist");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("receptionist_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(x => x.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.WorkingShift)
                .HasColumnName("working_shift")
                .HasMaxLength(100);

            builder.HasIndex(x => x.AccountId)
                .IsUnique();

            builder.HasIndex(x => x.Phone)
                .IsUnique();

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.HasOne(x => x.Account)
                .WithOne(x => x.Receptionist)
                .HasForeignKey<Receptionist>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // CENTER MANAGER
        // =========================================================

        modelBuilder.Entity<CenterManager>(builder =>
        {
            builder.ToTable("center_manager");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("manager_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(x => x.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(150)
                .IsRequired();

            builder.HasIndex(x => x.AccountId)
                .IsUnique();

            builder.HasIndex(x => x.Phone)
                .IsUnique();

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.HasOne(x => x.Account)
                .WithOne(x => x.CenterManager)
                .HasForeignKey<CenterManager>(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // MEMBERSHIP PACKAGE
        // =========================================================

        modelBuilder.Entity<MembershipPackage>(builder =>
        {
            builder.ToTable("membership_package");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("package_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.PackageName)
                .HasColumnName("package_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(x => x.DurationDays)
                .HasColumnName("duration_days")
                .IsRequired();

            builder.Property(x => x.Price)
                .HasColumnName("price")
                .HasColumnType("numeric(12,2)")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.HasIndex(x => x.PackageName)
                .IsUnique();

            builder.HasIndex(x => x.Status);
        });


        // =========================================================
        // MEMBER MEMBERSHIP
        // =========================================================

        modelBuilder.Entity<Membership>(builder =>
        {
            builder.ToTable("member_membership");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("member_membership_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.MemberId)
                .HasColumnName("member_id")
                .IsRequired();

            builder.Property(x => x.PackageId)
                .HasColumnName("package_id")
                .IsRequired();

            builder.Property(x => x.StartDate)
                .HasColumnName("start_date")
                .IsRequired();

            builder.Property(x => x.EndDate)
                .HasColumnName("end_date")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.HasIndex(x => x.MemberId);

            builder.HasIndex(x => x.PackageId);

            builder.HasOne(x => x.Member)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Package)
                .WithMany(x => x.Memberships)
                .HasForeignKey(x => x.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.MemberId,
                x.PackageId
            });
        });


        // =========================================================
        // INVOICE
        // =========================================================

        modelBuilder.Entity<Invoice>(builder =>
        {
            builder.ToTable("invoice");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("invoice_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.ReceptionistId)
                .HasColumnName("receptionist_id")
                .IsRequired();

            builder.Property(x => x.MemberMembershipId)
                .HasColumnName("member_membership_id")
                .IsRequired();

            builder.Property(x => x.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(12,2)")
                .IsRequired();

            builder.Property(x => x.PaymentMethod)
                .HasColumnName("payment_method")
                .IsRequired();

            builder.Property(x => x.PaymentDate)
                .HasColumnName("payment_date")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.HasIndex(x => x.ReceptionistId);

            builder.HasIndex(x => x.MemberMembershipId);

            builder.HasIndex(x => x.Status);

            builder.HasOne(x => x.Receptionist)
                .WithMany(x => x.Invoices)
                .HasForeignKey(x => x.ReceptionistId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Membership)
                .WithOne(x => x.Invoice)
                .HasForeignKey<Invoice>(x => x.MemberMembershipId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // SPORT CATEGORY
        // =========================================================

        modelBuilder.Entity<SportCategory>(builder =>
        {
            builder.ToTable("sport_category");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("category_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.CategoryName)
                .HasColumnName("category_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.HasIndex(x => x.CategoryName)
                .IsUnique();
        });


        // =========================================================
        // CLASS
        // =========================================================

        modelBuilder.Entity<Class>(builder =>
        {
            builder.ToTable("class");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("class_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.ClassName)
                .HasColumnName("class_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            builder.Property(x => x.CoachId)
                .HasColumnName("coach_id")
                .IsRequired();

            builder.Property(x => x.MaxCapacity)
                .HasColumnName("max_capacity")
                .IsRequired();

            builder.Property(x => x.StartDate)
                .HasColumnName("start_date")
                .IsRequired();

            builder.Property(x => x.EndDate)
                .HasColumnName("end_date")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.HasIndex(x => x.CategoryId);

            builder.HasIndex(x => x.CoachId);

            builder.HasIndex(x => x.Status);

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Classes)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Coach)
                .WithMany(x => x.Classes)
                .HasForeignKey(x => x.CoachId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // CLASS SESSION
        // =========================================================

        modelBuilder.Entity<ClassSession>(builder =>
        {
            builder.ToTable("class_session");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("session_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.ClassId)
                .HasColumnName("class_id")
                .IsRequired();

            builder.Property(x => x.RoomId)
                .HasColumnName("room_id")
                .IsRequired();

            builder.Property(x => x.Date)
                .HasColumnName("date")
                .IsRequired();

            builder.Property(x => x.StartTime)
                .HasColumnName("start_time")
                .IsRequired();

            builder.Property(x => x.EndTime)
                .HasColumnName("end_time")
                .IsRequired();

            builder.HasIndex(x => x.ClassId);

            builder.HasIndex(x => x.RoomId);

            builder.HasIndex(x => new
            {
                x.RoomId,
                x.Date,
                x.StartTime
            });

            builder.HasOne(x => x.Class)
                .WithMany(x => x.ClassSessions)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Room)
                .WithMany(x => x.ClassSessions)
                .HasForeignKey(x => x.RoomId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // CLASS BOOKING
        // =========================================================

        modelBuilder.Entity<ClassBooking>(builder =>
        {
            builder.ToTable("class_booking");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("booking_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.MemberId)
                .HasColumnName("member_id")
                .IsRequired();

            builder.Property(x => x.ClassId)
                .HasColumnName("class_id")
                .IsRequired();

            builder.Property(x => x.BookingDate)
                .HasColumnName("booking_date")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.HasIndex(x => x.MemberId);

            builder.HasIndex(x => x.ClassId);

            // Một member không book cùng một class nhiều lần
            builder.HasIndex(x => new
            {
                x.MemberId,
                x.ClassId
            }).IsUnique();

            builder.HasOne(x => x.Member)
                .WithMany(x => x.ClassBookings)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Class)
                .WithMany(x => x.ClassBookings)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // ATTENDANCE
        // =========================================================

        modelBuilder.Entity<Attendance>(builder =>
        {
            builder.ToTable("attendance");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("attendance_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.SessionId)
                .HasColumnName("session_id")
                .IsRequired();

            builder.Property(x => x.MemberId)
                .HasColumnName("member_id")
                .IsRequired();

            builder.Property(x => x.RecordedBy)
                .HasColumnName("recorded_by")
                .IsRequired();

            builder.Property(x => x.CheckInTime)
                .HasColumnName("check_in_time")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.HasIndex(x => x.SessionId);

            builder.HasIndex(x => x.MemberId);

            builder.HasIndex(x => new
            {
                x.SessionId,
                x.MemberId
            }).IsUnique();

            builder.HasOne(x => x.Session)
                .WithMany(x => x.Attendances)
                .HasForeignKey(x => x.SessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Member)
                .WithMany(x => x.Attendances)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TRAINING PLAN
        // =========================================================

        modelBuilder.Entity<TrainingPlan>(builder =>
        {
            builder.ToTable("training_plan");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("plan_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.CoachId)
                .HasColumnName("coach_id")
                .IsRequired();

            builder.Property(x => x.MemberId)
                .HasColumnName("member_id")
                .IsRequired();

            builder.Property(x => x.ClassId)
                .HasColumnName("class_id")
                .IsRequired();

            builder.Property(x => x.Title)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.ContentDescription)
                .HasColumnName("content_description")
                .HasColumnType("text");

            builder.Property(x => x.CreatedDate)
                .HasColumnName("created_date")
                .IsRequired();

            builder.HasIndex(x => x.CoachId);

            builder.HasIndex(x => x.MemberId);

            builder.HasIndex(x => x.ClassId);

            builder.HasOne(x => x.Coach)
                .WithMany(x => x.TrainingPlans)
                .HasForeignKey(x => x.CoachId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Member)
                .WithMany(x => x.TrainingPlans)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Class)
                .WithMany(x => x.TrainingPlans)
                .HasForeignKey(x => x.ClassId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // TRAINING RESULT
        // =========================================================

        modelBuilder.Entity<TrainingResult>(builder =>
        {
            builder.ToTable("training_result");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("result_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.PlanId)
                .HasColumnName("plan_id")
                .IsRequired();

            builder.Property(x => x.MemberId)
                .HasColumnName("member_id")
                .IsRequired();

            builder.Property(x => x.CoachId)
                .HasColumnName("coach_id")
                .IsRequired();

            builder.Property(x => x.PerformanceScore)
                .HasColumnName("performance_score")
                .IsRequired();

            builder.Property(x => x.CoachComment)
                .HasColumnName("coach_comment")
                .HasColumnType("text");

            builder.Property(x => x.EvaluationDate)
                .HasColumnName("evaluation_date")
                .IsRequired();

            builder.HasIndex(x => x.PlanId);

            builder.HasIndex(x => x.MemberId);

            builder.HasIndex(x => x.CoachId);

            builder.HasOne(x => x.Plan)
                .WithMany(x => x.TrainingResults)
                .HasForeignKey(x => x.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Member)
                .WithMany(x => x.TrainingResults)
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Coach)
                .WithMany(x => x.TrainingResults)
                .HasForeignKey(x => x.CoachId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // AUDIT LOG
        // =========================================================

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_log");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("audit_log_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.AccountId)
                .HasColumnName("account_id")
                .IsRequired();

            builder.Property(x => x.ActionType)
                .HasColumnName("action_type")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(x => x.Timestamp)
                .HasColumnName("timestamp")
                .HasDefaultValueSql("now()")
                .IsRequired();

            builder.HasIndex(x => x.AccountId);

            builder.HasIndex(x => x.Timestamp);

            builder.HasOne(x => x.Account)
                .WithMany(x => x.AuditLogs)
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });


        // =========================================================
        // ROOM
        // =========================================================

        modelBuilder.Entity<Room>(builder =>
        {
            builder.ToTable("room");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("room_id")
                .HasDefaultValueSql("gen_random_uuid()");

            builder.Property(x => x.RoomName)
                .HasColumnName("room_name")
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Capacity)
                .HasColumnName("capacity")
                .IsRequired();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            builder.HasIndex(x => x.RoomName)
                .IsUnique();

            builder.HasIndex(x => x.Status);
        });
    }
}