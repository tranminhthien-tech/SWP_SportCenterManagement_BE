using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations;

// FIX 2026-10-22: tách thông tin cá nhân khỏi vòng đời hồ sơ nhân sự.
[DbContext(typeof(AppDbContext))]
[Migration("20261022010000_PreserveAccountContact")]
public class PreserveAccountContact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "full_name", table: "account",
            type: "character varying(150)", maxLength: 150, nullable: false, defaultValue: "");
        migrationBuilder.AddColumn<string>(name: "phone", table: "account",
            type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "");

        // FIX: backfill từ hồ sơ còn tồn tại theo đúng role; không đoán thông tin đã bị xóa.
        migrationBuilder.Sql(@"
            UPDATE account a SET full_name = p.full_name, phone = p.phone
            FROM (
                SELECT account_id, full_name, phone, 1 AS role FROM member
                UNION ALL SELECT account_id, full_name, phone, 2 FROM coach
                UNION ALL SELECT account_id, full_name, phone, 3 FROM receptionist
                UNION ALL SELECT account_id, full_name, phone, 4 FROM center_manager
            ) p
            WHERE a.account_id = p.account_id AND a.role = p.role;
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "full_name", table: "account");
        migrationBuilder.DropColumn(name: "phone", table: "account");
    }
}
