using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SWP_SportCenter.Repository.Migrations;

// FIX B18: dữ liệu chưa biết lưu null thay vì số điện thoại/ngày sinh giả.
[DbContext(typeof(AppDbContext))]
[Migration("20261022020000_MemberUnknownContact")]
public class MemberUnknownContact : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(name: "phone", table: "member", type: "character varying(20)",
            maxLength: 20, nullable: true, oldClrType: typeof(string), oldType: "character varying(20)", oldMaxLength: 20);
        migrationBuilder.AlterColumn<DateTime>(name: "dob", table: "member", type: "timestamp with time zone",
            nullable: true, oldClrType: typeof(DateTime), oldType: "timestamp with time zone");
        // FIX: chỉ xóa ngày sinh giả khi đi kèm dấu hiệu phone placeholder, không đoán dữ liệu thật.
        migrationBuilder.Sql(@"
            UPDATE member SET dob = NULL
            WHERE phone LIKE 'legacy-%'
               OR ((phone LIKE 'pending-%' OR phone = '') AND dob::date = ""CreatedAt""::date);
            UPDATE account SET phone = '' WHERE phone LIKE 'pending-%' OR phone LIKE 'legacy-%';
            UPDATE member SET phone = NULL WHERE phone LIKE 'pending-%' OR phone LIKE 'legacy-%' OR phone = '';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // FIX: không rollback bằng cách tái tạo dữ liệu cá nhân giả.
        throw new NotSupportedException("Không thể rollback cột nullable nếu chưa bổ sung đầy đủ phone/dob thật.");
    }
}
