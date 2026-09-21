using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class MembershipPackage: BaseEntity, IAuditableEntity
{
    
    public string PackageName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int DurationDays { get; set; }

    public decimal Price { get; set; }

    public MembershipStatus Status { get; set; }

    // Navigation
    public ICollection<MemberMembership> MemberMemberships { get; set; }
        = new List<MemberMembership>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}