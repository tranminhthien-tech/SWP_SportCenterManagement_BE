using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class SecurePaymentsAndProfileImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FIX B22 B39: ảnh staff và metadata thanh toán online. Không suy đoán giao dịch online cũ.
            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "receptionist",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "receptionist_id",
                table: "invoice",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "payment_date",
                table: "invoice",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "payment_expires_at",
                table: "invoice",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Avatar",
                table: "center_manager",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // FIX B39: không rollback bằng cách bịa lễ tân hoặc ngày thanh toán cho dữ liệu null.
            migrationBuilder.Sql("""
                DO $$ BEGIN
                  IF EXISTS (SELECT 1 FROM invoice WHERE receptionist_id IS NULL OR payment_date IS NULL) THEN
                    RAISE EXCEPTION 'Cannot rollback nullable payment metadata while invoices have unknown collector or paid date';
                  END IF;
                END $$;
                """);
            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "receptionist");

            migrationBuilder.DropColumn(
                name: "payment_expires_at",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "Avatar",
                table: "center_manager");

            migrationBuilder.AlterColumn<Guid>(
                name: "receptionist_id",
                table: "invoice",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "payment_date",
                table: "invoice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
