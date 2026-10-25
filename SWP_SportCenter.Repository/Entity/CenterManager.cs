using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class CenterManager: BaseEntity, IAuditableEntity
{
    public string? Avatar { get; set; } // FIX B22: URL ảnh cloud.
    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;



    // Navigation
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
