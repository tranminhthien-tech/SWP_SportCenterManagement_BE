using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class Notification: BaseEntity, IAuditableEntity
{
    // FIX B32: null cho thông báo thường; giữ khóa nhắc hạn ngay cả khi xóa mềm.
    public string? DeduplicationKey { get; set; }
    

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; }
    

    // Navigation
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
