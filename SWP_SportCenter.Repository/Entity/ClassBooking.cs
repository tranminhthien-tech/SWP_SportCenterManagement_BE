using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class ClassBooking: BaseEntity, IAuditableEntity
{
    public decimal OriginalPrice { get; set; } // FIX B53: snapshot giá lúc đặt.
    public int DiscountPercent { get; set; }
    public decimal Price { get; set; }
    public Guid? MembershipId { get; set; }
    public Membership? Membership { get; set; }
    public DateTimeOffset? PaymentExpiresAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public Invoice? Invoice { get; set; }
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
