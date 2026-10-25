using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Invoice: BaseEntity, IAuditableEntity
{
    // CODE CŨ: public Guid ReceptionistId { get; set; }
    // FIX B39: online không có nhân viên thu tiền, không gán ngẫu nhiên lễ tân.
    public Guid? ReceptionistId { get; set; }

    public Guid MemberMembershipId { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    // CODE CŨ: public DateTimeOffset PaymentDate { get; set; }
    // FIX B39: chỉ có ngày trả tiền khi Paid; thời hạn chỉ áp dụng checkout online.
    public DateTimeOffset? PaymentDate { get; set; }
    public DateTimeOffset? PaymentExpiresAt { get; set; }

    public InvoiceStatus Status { get; set; }

    // Navigation

    // CODE CŨ: public Receptionist Receptionist { get; set; } = null!;
    public Receptionist? Receptionist { get; set; } // FIX B39

    public Membership Membership { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
