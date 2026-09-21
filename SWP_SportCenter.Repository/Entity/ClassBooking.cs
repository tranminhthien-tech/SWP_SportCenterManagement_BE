using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class ClassBooking: BaseEntity, IAuditableEntity
{
    public Guid MemberId { get; set; }

    public Guid ClassId { get; set; }

    public DateTimeOffset BookingDate { get; set; }

    public BookingStatus Status { get; set; }

    // Navigation

    public Member Member { get; set; } = null!;

    public Class Class { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}