using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class SportCategory: BaseEntity, IAuditableEntity
{
    public string CategoryName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Navigation

    public ICollection<Class> Classes { get; set; }
        = new List<Class>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}