using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    password = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "membership_package",
                columns: table => new
                {
                    package_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    package_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    duration_days = table.Column<int>(type: "integer", nullable: false),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_package", x => x.package_id);
                });

            migrationBuilder.CreateTable(
                name: "room",
                columns: table => new
                {
                    room_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    room_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_room", x => x.room_id);
                });

            migrationBuilder.CreateTable(
                name: "sport_category",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    category_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sport_category", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "audit_log",
                columns: table => new
                {
                    audit_log_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_log", x => x.audit_log_id);
                    table.ForeignKey(
                        name: "FK_audit_log_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "center_manager",
                columns: table => new
                {
                    manager_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_center_manager", x => x.manager_id);
                    table.ForeignKey(
                        name: "FK_center_manager_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "coach",
                columns: table => new
                {
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    specialization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    experience_years = table.Column<int>(type: "integer", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_coach", x => x.coach_id);
                    table.ForeignKey(
                        name: "FK_coach_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member",
                columns: table => new
                {
                    member_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    dob = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gender = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    avatar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    training_goal = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member", x => x.member_id);
                    table.ForeignKey(
                        name: "FK_member_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notification",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notification", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_notification_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "receptionist",
                columns: table => new
                {
                    receptionist_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    full_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    working_shift = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receptionist", x => x.receptionist_id);
                    table.ForeignKey(
                        name: "FK_receptionist_account_account_id",
                        column: x => x.account_id,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class",
                columns: table => new
                {
                    class_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    class_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    max_capacity = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class", x => x.class_id);
                    table.ForeignKey(
                        name: "FK_class_coach_coach_id",
                        column: x => x.coach_id,
                        principalTable: "coach",
                        principalColumn: "coach_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_sport_category_category_id",
                        column: x => x.category_id,
                        principalTable: "sport_category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member_membership",
                columns: table => new
                {
                    member_membership_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_membership", x => x.member_membership_id);
                    table.ForeignKey(
                        name: "FK_member_membership_member_member_id",
                        column: x => x.member_id,
                        principalTable: "member",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_member_membership_membership_package_package_id",
                        column: x => x.package_id,
                        principalTable: "membership_package",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class_booking",
                columns: table => new
                {
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_booking", x => x.booking_id);
                    table.ForeignKey(
                        name: "FK_class_booking_class_class_id",
                        column: x => x.class_id,
                        principalTable: "class",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_booking_member_member_id",
                        column: x => x.member_id,
                        principalTable: "member",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "class_session",
                columns: table => new
                {
                    session_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    room_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_session", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_class_session_class_class_id",
                        column: x => x.class_id,
                        principalTable: "class",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_class_session_room_room_id",
                        column: x => x.room_id,
                        principalTable: "room",
                        principalColumn: "room_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "training_plan",
                columns: table => new
                {
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    class_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content_description = table.Column<string>(type: "text", nullable: false),
                    created_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_plan", x => x.plan_id);
                    table.ForeignKey(
                        name: "FK_training_plan_class_class_id",
                        column: x => x.class_id,
                        principalTable: "class",
                        principalColumn: "class_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_plan_coach_coach_id",
                        column: x => x.coach_id,
                        principalTable: "coach",
                        principalColumn: "coach_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_plan_member_member_id",
                        column: x => x.member_id,
                        principalTable: "member",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice",
                columns: table => new
                {
                    invoice_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    receptionist_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    payment_method = table.Column<int>(type: "integer", nullable: false),
                    payment_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice", x => x.invoice_id);
                    table.ForeignKey(
                        name: "FK_invoice_member_membership_member_membership_id",
                        column: x => x.member_membership_id,
                        principalTable: "member_membership",
                        principalColumn: "member_membership_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoice_receptionist_receptionist_id",
                        column: x => x.receptionist_id,
                        principalTable: "receptionist",
                        principalColumn: "receptionist_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attendance",
                columns: table => new
                {
                    attendance_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    check_in_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendance", x => x.attendance_id);
                    table.ForeignKey(
                        name: "FK_attendance_class_session_session_id",
                        column: x => x.session_id,
                        principalTable: "class_session",
                        principalColumn: "session_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_attendance_member_member_id",
                        column: x => x.member_id,
                        principalTable: "member",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "training_result",
                columns: table => new
                {
                    result_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    performance_score = table.Column<float>(type: "real", nullable: false),
                    coach_comment = table.Column<string>(type: "text", nullable: false),
                    evaluation_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_training_result", x => x.result_id);
                    table.ForeignKey(
                        name: "FK_training_result_coach_coach_id",
                        column: x => x.coach_id,
                        principalTable: "coach",
                        principalColumn: "coach_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_result_member_member_id",
                        column: x => x.member_id,
                        principalTable: "member",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_training_result_training_plan_plan_id",
                        column: x => x.plan_id,
                        principalTable: "training_plan",
                        principalColumn: "plan_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_role",
                table: "account",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "IX_account_status",
                table: "account",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_account_username",
                table: "account",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendance_member_id",
                table: "attendance",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_session_id",
                table: "attendance",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "IX_attendance_session_id_member_id",
                table: "attendance",
                columns: new[] { "session_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_account_id",
                table: "audit_log",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_audit_log_timestamp",
                table: "audit_log",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_center_manager_account_id",
                table: "center_manager",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_center_manager_email",
                table: "center_manager",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_center_manager_phone",
                table: "center_manager",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_category_id",
                table: "class",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_coach_id",
                table: "class",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_status",
                table: "class",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_class_booking_class_id",
                table: "class_booking",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_booking_member_id",
                table: "class_booking",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_booking_member_id_class_id",
                table: "class_booking",
                columns: new[] { "member_id", "class_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_session_class_id",
                table: "class_session",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_session_room_id",
                table: "class_session",
                column: "room_id");

            migrationBuilder.CreateIndex(
                name: "IX_class_session_room_id_date_start_time",
                table: "class_session",
                columns: new[] { "room_id", "date", "start_time" });

            migrationBuilder.CreateIndex(
                name: "IX_coach_account_id",
                table: "coach",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coach_email",
                table: "coach",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coach_phone",
                table: "coach",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_member_membership_id",
                table: "invoice",
                column: "member_membership_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_receptionist_id",
                table: "invoice",
                column: "receptionist_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_status",
                table: "invoice",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_member_account_id",
                table: "member",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_email",
                table: "member",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_phone",
                table: "member",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_membership_member_id",
                table: "member_membership",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_member_membership_member_id_package_id",
                table: "member_membership",
                columns: new[] { "member_id", "package_id" });

            migrationBuilder.CreateIndex(
                name: "IX_member_membership_package_id",
                table: "member_membership",
                column: "package_id");

            migrationBuilder.CreateIndex(
                name: "IX_membership_package_package_name",
                table: "membership_package",
                column: "package_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_membership_package_status",
                table: "membership_package",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_notification_account_id",
                table: "notification",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_notification_is_read",
                table: "notification",
                column: "is_read");

            migrationBuilder.CreateIndex(
                name: "IX_receptionist_account_id",
                table: "receptionist",
                column: "account_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receptionist_email",
                table: "receptionist",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receptionist_phone",
                table: "receptionist",
                column: "phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_room_name",
                table: "room",
                column: "room_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_room_status",
                table: "room",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_sport_category_category_name",
                table: "sport_category",
                column: "category_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_training_plan_class_id",
                table: "training_plan",
                column: "class_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_plan_coach_id",
                table: "training_plan",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_plan_member_id",
                table: "training_plan",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_result_coach_id",
                table: "training_result",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_result_member_id",
                table: "training_result",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "IX_training_result_plan_id",
                table: "training_result",
                column: "plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance");

            migrationBuilder.DropTable(
                name: "audit_log");

            migrationBuilder.DropTable(
                name: "center_manager");

            migrationBuilder.DropTable(
                name: "class_booking");

            migrationBuilder.DropTable(
                name: "invoice");

            migrationBuilder.DropTable(
                name: "notification");

            migrationBuilder.DropTable(
                name: "training_result");

            migrationBuilder.DropTable(
                name: "class_session");

            migrationBuilder.DropTable(
                name: "member_membership");

            migrationBuilder.DropTable(
                name: "receptionist");

            migrationBuilder.DropTable(
                name: "training_plan");

            migrationBuilder.DropTable(
                name: "room");

            migrationBuilder.DropTable(
                name: "membership_package");

            migrationBuilder.DropTable(
                name: "class");

            migrationBuilder.DropTable(
                name: "member");

            migrationBuilder.DropTable(
                name: "coach");

            migrationBuilder.DropTable(
                name: "sport_category");

            migrationBuilder.DropTable(
                name: "account");
        }
    }
}
