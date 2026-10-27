using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class MembershipEligibilityCheckInsAndTrainingSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FIX B46 B47 B51: chỉ bổ sung schema, không suy diễn môn hoặc sửa booking/profile cũ.
            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "training_result",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "check_in",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceptionistId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedByAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    CheckInTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_check_in", x => x.Id);
                    table.ForeignKey(
                        name: "FK_check_in_account_RecordedByAccountId",
                        column: x => x.RecordedByAccountId,
                        principalTable: "account",
                        principalColumn: "account_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_check_in_member_MemberId",
                        column: x => x.MemberId,
                        principalTable: "member",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_check_in_member_membership_MembershipId",
                        column: x => x.MembershipId,
                        principalTable: "member_membership",
                        principalColumn: "member_membership_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_check_in_receptionist_ReceptionistId",
                        column: x => x.ReceptionistId,
                        principalTable: "receptionist",
                        principalColumn: "receptionist_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "membership_package_category",
                columns: table => new
                {
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_package_category", x => new { x.PackageId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_membership_package_category_membership_package_PackageId",
                        column: x => x.PackageId,
                        principalTable: "membership_package",
                        principalColumn: "package_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_membership_package_category_sport_category_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "sport_category",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_training_result_SessionId",
                table: "training_result",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_CheckInTime",
                table: "check_in",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_MemberId_CheckInTime",
                table: "check_in",
                columns: new[] { "MemberId", "CheckInTime" });

            migrationBuilder.CreateIndex(
                name: "IX_check_in_MembershipId",
                table: "check_in",
                column: "MembershipId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_ReceptionistId",
                table: "check_in",
                column: "ReceptionistId");

            migrationBuilder.CreateIndex(
                name: "IX_check_in_RecordedByAccountId",
                table: "check_in",
                column: "RecordedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_membership_package_category_CategoryId",
                table: "membership_package_category",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_training_result_class_session_SessionId",
                table: "training_result",
                column: "SessionId",
                principalTable: "class_session",
                principalColumn: "session_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // FIX: rollback xóa dữ liệu bảng/cột mới; phải sao lưu trước khi chạy trên DB thật.
            migrationBuilder.DropForeignKey(
                name: "FK_training_result_class_session_SessionId",
                table: "training_result");

            migrationBuilder.DropTable(
                name: "check_in");

            migrationBuilder.DropTable(
                name: "membership_package_category");

            migrationBuilder.DropIndex(
                name: "IX_training_result_SessionId",
                table: "training_result");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "training_result");
        }
    }
}
