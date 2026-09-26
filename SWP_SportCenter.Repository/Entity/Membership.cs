using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Membership: BaseEntity, IAuditableEntity
{
    public Guid MemberId { get; set; }

    public Guid PackageId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public MembershipStatus Status { get; set; }

    // Navigation

    public Member Member { get; set; } = null!;

    public MembershipPackage Package { get; set; } = null!;

    public Invoice? Invoice { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}