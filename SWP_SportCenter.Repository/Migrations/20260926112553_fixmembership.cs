using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class fixmembership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "training_result",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "training_plan",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "sport_category",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "room",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "receptionist",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "notification",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "membership_package",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "member_membership",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "member",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "invoice",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "coach",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "class_session",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "class_booking",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "class",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "center_manager",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "audit_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "attendance",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "account",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "training_result");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "training_plan");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "sport_category");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "room");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "receptionist");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "notification");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "membership_package");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "member_membership");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "member");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "invoice");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "coach");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "class_session");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "class_booking");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "class");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "center_manager");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "audit_log");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "attendance");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "account");
        }
    }
}
