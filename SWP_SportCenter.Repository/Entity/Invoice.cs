using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Invoice: BaseEntity, IAuditableEntity
{
    public Guid ReceptionistId { get; set; }

    public Guid MemberMembershipId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public DateTimeOffset PaymentDate { get; set; }

    public InvoiceStatus Status { get; set; }

    // Navigation

    public Receptionist Receptionist { get; set; } = null!;

    public MemberMembership MemberMembership { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}