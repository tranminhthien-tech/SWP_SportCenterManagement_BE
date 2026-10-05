using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class addaccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "account",
                columns: new[] { "account_id", "created_at", "Email", "IsDeleted", "IsVerify", "password", "role", "status", "updated_at", "username" },
                values: new object[,]
                {
                    { new Guid("55555555-5555-5555-5555-555555555555"), new DateTimeOffset(new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "binh.tran.yoga@sportcenter.com", false, true, "XjPY2ddfUWTRs09lvSDvww==:lBsRcZuZNY/eGQr07s1mig==", 2, 1, null, "tran.thanh.binh" },
                    { new Guid("66666666-6666-6666-6666-666666666666"), new DateTimeOffset(new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "hung.nguyen.fitness@sportcenter.com", false, true, "XjPY2ddfUWTRs09lvSDvww==:lBsRcZuZNY/eGQr07s1mig==", 2, 1, null, "nguyen.manh.hung" },
                    { new Guid("77777777-7777-7777-7777-777777777777"), new DateTimeOffset(new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "yen.le.swim@sportcenter.com", false, true, "XjPY2ddfUWTRs09lvSDvww==:lBsRcZuZNY/eGQr07s1mig==", 2, 1, null, "le.hoang.yen" }
                });

            migrationBuilder.InsertData(
                table: "coach",
                columns: new[] { "coach_id", "account_id", "avatar", "CreatedAt", "email", "experience_years", "full_name", "IsDeleted", "phone", "specialization", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new Guid("55555555-5555-5555-5555-555555555555"), "", new DateTimeOffset(new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "binh.tran.yoga@sportcenter.com", 8, "Trần Thanh Bình", false, "0901234567", "Yoga Trị Liệu & Pilates", null },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new Guid("66666666-6666-6666-6666-666666666666"), "", new DateTimeOffset(new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "hung.nguyen.fitness@sportcenter.com", 4, "Nguyễn Mạnh Hùng", false, "0987654321", "Huấn luyện Thể hình & Ép mỡ cấp tốc", null },
                    { new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"), new Guid("77777777-7777-7777-7777-777777777777"), "", new DateTimeOffset(new DateTime(2026, 10, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "yen.le.swim@sportcenter.com", 1, "Lê Hoàng Yến", false, "0912345678", "Bơi ếch, Bơi sải cơ bản cho trẻ em", null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"));

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"));

            migrationBuilder.DeleteData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"));
        }
    }
}
