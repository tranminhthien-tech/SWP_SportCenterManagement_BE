using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SWP_SportCenter.Repository.Entity;

namespace SWP_SportCenter.Infrastructure;

// FIX B33: audit được ghi cùng transaction với thay đổi, danh tính chỉ lấy từ token.
// FIX B35: sinh Guid ở ứng dụng trước khi lưu để entityId trong log Added luôn là ID thật.
// FIX B35: ghi chi tiết giá trị cũ → mới (changes), loại trừ các trường nhạy cảm.
public sealed class ServerAuditInterceptor(IHttpContextAccessor http) : SaveChangesInterceptor
{
    private static bool IsSensitive(string name) =>
        name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Token", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Hash", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Salt", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("Email", StringComparison.OrdinalIgnoreCase);

    private void Capture(DbContext? db)
    {
        if (db == null) return;

        // FIX B35: Đảm bảo mọi entity Added có khóa chính Guid thật trước khi đọc ID ghi log.
        foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added))
        {
            var pk = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            if (pk != null && pk.Metadata.ClrType == typeof(Guid))
            {
                if (pk.CurrentValue == null || (Guid)pk.CurrentValue == Guid.Empty || pk.IsTemporary)
                {
                    pk.CurrentValue = Guid.NewGuid();
                    pk.IsTemporary = false;
                }
            }
        }

        var user = http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true ||
            !new[] { "Coach", "Receptionist", "CenterManager" }.Any(user.IsInRole) ||
            !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return;
        db.ChangeTracker.DetectChanges();

        var changes = db.ChangeTracker.Entries().Where(e => e.Entity is not AuditLog &&
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();

        foreach (var entry in changes)
        {
            var id = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue;
            var changeMap = new Dictionary<string, object?>();
            var changedFieldsList = new List<string>();

            if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.Properties)
                {
                    var name = prop.Metadata.Name;
                    if (prop.Metadata.IsPrimaryKey() || IsSensitive(name)) continue;
                    changeMap[name] = new { old = (object?)null, @new = prop.CurrentValue };
                    changedFieldsList.Add(name);
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                // CODE CŨ: foreach (var prop in entry.Properties.Where(p => p.IsModified))
                // FIX B40: Update(entity) đánh dấu mọi cột; so bằng ValueComparer của EF.
                foreach (var prop in entry.Properties.Where(p => p.IsModified &&
                             !(p.Metadata.GetValueComparer()?.Equals(p.OriginalValue, p.CurrentValue)
                               ?? Equals(p.OriginalValue, p.CurrentValue))))
                {
                    var name = prop.Metadata.Name;
                    if (IsSensitive(name)) continue;
                    changeMap[name] = new { old = prop.OriginalValue, @new = prop.CurrentValue };
                    changedFieldsList.Add(name);
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var prop in entry.Properties)
                {
                    var name = prop.Metadata.Name;
                    if (prop.Metadata.IsPrimaryKey() || IsSensitive(name)) continue;
                    changeMap[name] = new { old = prop.OriginalValue, @new = (object?)null };
                    changedFieldsList.Add(name);
                }
            }

            // FIX B40: không tạo dòng Modified rỗng (vd. chỉ password bị loại vì nhạy cảm).
            if (entry.State == EntityState.Modified && changedFieldsList.Count == 0) continue;
            var auditPayload = new
            {
                entityId = id,
                changedFields = changedFieldsList.ToArray(),
                changes = changeMap,
                source = "server"
            };

            db.Set<AuditLog>().Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                AccountId = actorId,
                ActionType = $"{entry.State}:{entry.Metadata.ClrType.Name}",
                Description = JsonSerializer.Serialize(auditPayload),
                Timestamp = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData data, InterceptionResult<int> result)
    { Capture(data.Context); return result; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Capture(data.Context); return ValueTask.FromResult(result); }
}
