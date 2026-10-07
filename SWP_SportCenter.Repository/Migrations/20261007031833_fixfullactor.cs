using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class fixfullactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_receptionist_email",
                table: "receptionist");

            migrationBuilder.DropIndex(
                name: "IX_member_email",
                table: "member");

            migrationBuilder.DropIndex(
                name: "IX_coach_email",
                table: "coach");

            migrationBuilder.DropIndex(
                name: "IX_center_manager_email",
                table: "center_manager");

            migrationBuilder.DropColumn(
                name: "email",
                table: "receptionist");

            migrationBuilder.DropColumn(
                name: "email",
                table: "member");

            migrationBuilder.DropColumn(
                name: "email",
                table: "coach");

            migrationBuilder.DropColumn(
                name: "email",
                table: "center_manager");

            migrationBuilder.DropColumn(
                name: "IsVerify",
                table: "account");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "account",
                newName: "email");

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "account",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "password",
                value: "");

            migrationBuilder.CreateIndex(
                name: "IX_account_email",
                table: "account",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_account_email",
                table: "account");

            migrationBuilder.RenameColumn(
                name: "email",
                table: "account",
                newName: "Email");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "receptionist",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "member",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "coach",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "center_manager",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "account",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerify",
                table: "account",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "IsVerify", "password" },
                values: new object[] { true, "XjPY2ddfUWTRs09lvSDvww==:lBsRcZuZNY/eGQr07s1mig==" });

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "IsVerify",
                value: true);

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                column: "IsVerify",
                value: true);

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("44444444-4444-4444-4444-444444444444"),
                column: "IsVerify",
                value: true);

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"),
                column: "IsVerify",
                value: true);

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("66666666-6666-6666-6666-666666666666"),
                column: "IsVerify",
                value: true);

            migrationBuilder.UpdateData(
                table: "account",
                keyColumn: "account_id",
                keyValue: new Guid("77777777-7777-7777-7777-777777777777"),
                column: "IsVerify",
                value: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_receptionist_email",
                table: "receptionist",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_email",
                table: "member",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_coach_email",
                table: "coach",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_center_manager_email",
                table: "center_manager",
                column: "email",
                unique: true);
        }
    }
}
