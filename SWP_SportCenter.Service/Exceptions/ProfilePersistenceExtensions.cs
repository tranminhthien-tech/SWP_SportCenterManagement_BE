using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace SWP_SportCenter.Service.Exceptions;

// FIX 2026-10-22: xử lý request tạo hồ sơ đồng thời vi phạm unique index.
internal static class ProfilePersistenceExtensions
{
    public static async Task SaveProfileChangesAsync(this DbContext context)
    {
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation &&
            pg.ConstraintName is "IX_coach_account_id" or "IX_receptionist_account_id" or
                "IX_center_manager_account_id" or "IX_coach_phone" or "IX_receptionist_phone" or
                "IX_center_manager_phone" or "IX_account_email")
        {
            var constraint = ((PostgresException)ex.InnerException!).ConstraintName!;
            throw new BusinessException(constraint.EndsWith("account_id")
                ? "Tài khoản này đã được gán hồ sơ trong hệ thống."
                : constraint.EndsWith("phone") ? "Số điện thoại này đã được sử dụng." : "Email này đã được sử dụng.");
        }
    }
}
