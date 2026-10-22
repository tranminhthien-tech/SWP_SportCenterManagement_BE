using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SWP_SportCenter.Repository.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMissingMemberProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FIX 2026-10-22: Member accounts created by the old register flow had no row in "member".
            // The old database has no saved full name, so username is used as a clearly traceable fallback.
            migrationBuilder.Sql(@"
                INSERT INTO member
                    -- CODE CŨ: is_deleted, created_at (không đúng tên cột EF của Member).
                    -- FIX: dùng tên cột chính xác trước khi chạy migration giữ thông tin Account.
                    (member_id, account_id, full_name, dob, gender, phone, avatar, training_goal, ""IsDeleted"", ""CreatedAt"")
                SELECT
                    gen_random_uuid(),
                    a.account_id,
                    a.username,
                    CURRENT_DATE,
                    '',
                    'legacy-' || substring(replace(a.account_id::text, '-', '') from 1 for 13),
                    '',
                    '',
                    false,
                    NOW()
                FROM account AS a
                LEFT JOIN member AS m ON m.account_id = a.account_id
                WHERE a.role = 1
                  -- CODE CŨ: a.is_deleted = false
                  -- FIX: Account dùng tên cột EF IsDeleted có phân biệt hoa/thường.
                  AND a.""IsDeleted"" = false
                  AND m.member_id IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không xóa dữ liệu đã backfill để tránh làm mất profile Member hợp lệ.
        }
    }
}
