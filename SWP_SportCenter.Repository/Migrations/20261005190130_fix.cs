using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "Email",
                value: "yoga@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "Email",
                value: "fitness@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "Email",
                value: "swim@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "email",
                value: "yoga@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "email",
                value: "fitness@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "email",
                value: "swim@sportcenter.com");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "Email",
                value: "binh.tran.yoga@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "Email",
                value: "hung.nguyen.fitness@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "Email",
                value: "yen.le.swim@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "email",
                value: "binh.tran.yoga@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "email",
                value: "hung.nguyen.fitness@sportcenter.com");

            migrationBuilder.UpdateData(
                table: "coach",
                keyColumn: "coach_id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "email",
                value: "yen.le.swim@sportcenter.com");
        }
    }
}
