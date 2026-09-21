using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class AuditLog: BaseEntity, IAuditableEntity
{
    public Guid AccountId { get; set; }

    public string ActionType { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset Timestamp { get; set; }

    // Navigation

    public Account Account { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}