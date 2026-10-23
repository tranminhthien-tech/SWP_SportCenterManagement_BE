using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SWP_SportCenter.Repository.Entity;

namespace SWP_SportCenter.Infrastructure;

// FIX B33: audit được ghi cùng transaction với thay đổi, danh tính chỉ lấy từ token.
public sealed class ServerAuditInterceptor(IHttpContextAccessor http) : SaveChangesInterceptor
{
    private void Capture(DbContext? db)
    {
        var user = http.HttpContext?.User;
        if (db == null || user?.Identity?.IsAuthenticated != true ||
            !new[] { "Coach", "Receptionist", "CenterManager" }.Any(user.IsInRole) ||
            !Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return;
        db.ChangeTracker.DetectChanges();
        var changes = db.ChangeTracker.Entries().Where(e => e.Entity is not AuditLog &&
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        foreach (var entry in changes)
        {
            // Không đưa giá trị password, email, token hay nội dung nhạy cảm vào audit.
            var fields = entry.Properties.Where(p => p.IsModified)
                .Select(p => p.Metadata.Name).Where(n =>
                    !n.Contains("Password", StringComparison.OrdinalIgnoreCase) &&
                    !n.Contains("Token", StringComparison.OrdinalIgnoreCase)).ToArray();
            var id = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue;
            db.Set<AuditLog>().Add(new AuditLog {
                Id = Guid.NewGuid(), AccountId = actorId,
                ActionType = $"{entry.State}:{entry.Metadata.ClrType.Name}",
                Description = JsonSerializer.Serialize(new { entityId = id, changedFields = fields, source = "server" }),
                Timestamp = DateTimeOffset.UtcNow, CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData data, InterceptionResult<int> result)
    { Capture(data.Context); return result; }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    { Capture(data.Context); return ValueTask.FromResult(result); }
}
