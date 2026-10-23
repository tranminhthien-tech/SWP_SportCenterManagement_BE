using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class StaffWorkflowSupportNotifications : Migration
    {
        // FIX B31 B32: migration bổ sung bảng yêu cầu hỗ trợ và khóa chống thông báo nhắc hạn trùng.
        // Không backfill/tự tạo hồ sơ nhân sự vì B26 đã chốt giữ hai bước.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "notification",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "support_request",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    HandledByAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_request", x => x.Id);
                    table.ForeignKey(
                        name: "FK_support_request_account_HandledByAccountId",
                        column: x => x.HandledByAccountId,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_support_request_member_MemberId",
                        column: x => x.MemberId,
                        principalTable: "member",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notification_DeduplicationKey",
                table: "notification",
                column: "DeduplicationKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_support_request_HandledByAccountId",
                table: "support_request",
                column: "HandledByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_support_request_MemberId_CreatedAt",
                table: "support_request",
                columns: new[] { "MemberId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_support_request_Status_CreatedAt",
                table: "support_request",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_request");

            migrationBuilder.DropIndex(
                name: "IX_notification_DeduplicationKey",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "notification");
        }
    }
}
