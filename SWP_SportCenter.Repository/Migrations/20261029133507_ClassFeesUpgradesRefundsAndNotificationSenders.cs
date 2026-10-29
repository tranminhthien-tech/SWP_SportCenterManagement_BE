using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class ClassFeesUpgradesRefundsAndNotificationSenders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_class_booking_member_id_class_id",
                table: "class_booking");

            migrationBuilder.AddColumn<Guid>(
                name: "SenderAccountId",
                table: "notification",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderName",
                table: "notification",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderRole",
                table: "notification",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Benefits",
                table: "membership_package",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "membership_package",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "UpgradedFromId",
                table: "member_membership",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "member_membership_id",
                table: "invoice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassBookingId",
                table: "invoice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditAmount",
                table: "invoice",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "invoice",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FromPackageName",
                table: "invoice",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "invoice",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Membership"); // FIX: hóa đơn cũ đều là Membership, không để kind rỗng.

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalAmount",
                table: "invoice",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "RefundConfirmedByAccountId",
                table: "invoice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReference",
                table: "invoice",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefundRequestedAt",
                table: "invoice",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefundedAt",
                table: "invoice",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpgradeFromMembershipId",
                table: "invoice",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpgradeSourceEndDate",
                table: "invoice",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WasExpired",
                table: "invoice",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CancelledAt",
                table: "class_booking",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "class_booking",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "MembershipId",
                table: "class_booking",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "class_booking",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PaymentExpiresAt",
                table: "class_booking",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "class_booking",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "class_booking",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "class",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_notification_SenderAccountId",
                table: "notification",
                column: "SenderAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_member_membership_UpgradedFromId",
                table: "member_membership",
                column: "UpgradedFromId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_ClassBookingId",
                table: "invoice",
                column: "ClassBookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoice_RefundConfirmedByAccountId",
                table: "invoice",
                column: "RefundConfirmedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_UpgradeFromMembershipId",
                table: "invoice",
                column: "UpgradeFromMembershipId");

            // FIX B53: giữ booking cũ giá 0, không tạo hóa đơn/thu thêm cho học viên đã đặt trước.
            migrationBuilder.Sql("UPDATE invoice SET \"Kind\" = 'Membership', \"OriginalAmount\" = amount WHERE \"ClassBookingId\" IS NULL;");
            // Chỉ backfill đúng ba gói nhóm đã chốt; không đoán giá/gói khác, không sửa thẻ đã mua.
            migrationBuilder.Sql("""
                UPDATE membership_package
                SET "DiscountPercent" = 10,
                    "Benefits" = LEFT(COALESCE((SELECT string_agg(regexp_replace(line, '^\s*-\s*', ''), E'\n' ORDER BY ord)
                        FROM regexp_split_to_table(COALESCE(description, ''), E'\r?\n') WITH ORDINALITY AS parts(line, ord) WHERE line ~ '^\s*-'), ''), 2000),
                    description = split_part(COALESCE(description, ''), E'\n', 1)
                WHERE package_name IN ('Gói 1 tháng', 'Gói 6 tháng', 'Gói 1 năm');
                """);
            migrationBuilder.AddCheckConstraint(
                name: "CK_invoice_subject",
                table: "invoice",
                sql: "(member_membership_id IS NOT NULL AND \"ClassBookingId\" IS NULL) OR (member_membership_id IS NULL AND \"ClassBookingId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_class_booking_member_id_class_id",
                table: "class_booking",
                columns: new[] { "member_id", "class_id" },
                unique: true,
                filter: "\"IsDeleted\" = false AND status <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_class_booking_MembershipId",
                table: "class_booking",
                column: "MembershipId");

            migrationBuilder.AddForeignKey(
                name: "FK_class_booking_member_membership_MembershipId",
                table: "class_booking",
                column: "MembershipId",
                principalTable: "member_membership",
                principalColumn: "member_membership_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_account_RefundConfirmedByAccountId",
                table: "invoice",
                column: "RefundConfirmedByAccountId",
                principalTable: "account",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_class_booking_ClassBookingId",
                table: "invoice",
                column: "ClassBookingId",
                principalTable: "class_booking",
                principalColumn: "booking_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_member_membership_UpgradeFromMembershipId",
                table: "invoice",
                column: "UpgradeFromMembershipId",
                principalTable: "member_membership",
                principalColumn: "member_membership_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_member_membership_member_membership_UpgradedFromId",
                table: "member_membership",
                column: "UpgradedFromId",
                principalTable: "member_membership",
                principalColumn: "member_membership_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_notification_account_SenderAccountId",
                table: "notification",
                column: "SenderAccountId",
                principalTable: "account",
                principalColumn: "account_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        /* CODE CŨ: rollback tự xóa dữ liệu tài chính mới rồi ép FK thẻ về NOT NULL.
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_booking_member_membership_MembershipId",
                table: "class_booking");

            migrationBuilder.DropForeignKey(
                name: "FK_invoice_account_RefundConfirmedByAccountId",
                table: "invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_invoice_class_booking_ClassBookingId",
                table: "invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_invoice_member_membership_UpgradeFromMembershipId",
                table: "invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_member_membership_member_membership_UpgradedFromId",
                table: "member_membership");

            migrationBuilder.DropForeignKey(
                name: "FK_notification_account_SenderAccountId",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "IX_notification_SenderAccountId",
                table: "notification");

            migrationBuilder.DropIndex(
                name: "IX_member_membership_UpgradedFromId",
                table: "member_membership");

            migrationBuilder.DropIndex(
                name: "IX_invoice_ClassBookingId",
                table: "invoice");

            migrationBuilder.DropIndex(
                name: "IX_invoice_RefundConfirmedByAccountId",
                table: "invoice");

            migrationBuilder.DropIndex(
                name: "IX_invoice_UpgradeFromMembershipId",
                table: "invoice");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoice_subject",
                table: "invoice");

            migrationBuilder.DropIndex(
                name: "IX_class_booking_member_id_class_id",
                table: "class_booking");

            migrationBuilder.DropIndex(
                name: "IX_class_booking_MembershipId",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "SenderAccountId",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "SenderName",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "SenderRole",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "Benefits",
                table: "membership_package");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "membership_package");

            migrationBuilder.DropColumn(
                name: "UpgradedFromId",
                table: "member_membership");

            migrationBuilder.DropColumn(
                name: "ClassBookingId",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "CreditAmount",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "FromPackageName",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "OriginalAmount",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "RefundConfirmedByAccountId",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "RefundReference",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "RefundRequestedAt",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "UpgradeFromMembershipId",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "UpgradeSourceEndDate",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "WasExpired",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "MembershipId",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "PaymentExpiresAt",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "class");

            migrationBuilder.AlterColumn<Guid>(
                name: "member_membership_id",
                table: "invoice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_booking_member_id_class_id",
                table: "class_booking",
                columns: new[] { "member_id", "class_id" },
                unique: true);
        }
        */
        protected override void Down(MigrationBuilder migrationBuilder)
            => throw new NotSupportedException("Không tự rollback hóa đơn lớp/nâng cấp/hoàn tiền. Phải sao lưu, đối soát và viết kế hoạch rollback dữ liệu trước.");
    }
}
