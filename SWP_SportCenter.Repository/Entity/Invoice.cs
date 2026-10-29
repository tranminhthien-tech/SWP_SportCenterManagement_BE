using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Invoice: BaseEntity, IAuditableEntity
{
    // FIX SEPAY: snapshot chỉ dẫn chuyển khoản; các hóa đơn cũ để null, không suy đoán đã trả tiền.
    public string? PaymentReference { get; set; }
    public string? SePayBankBin { get; set; }
    public string? SePayAccountNumber { get; set; }
    public string? SePayGateway { get; set; }
    public string? SePayQrCodeUrl { get; set; }
    // CODE CŨ: public Guid ReceptionistId { get; set; }
    // FIX B39: online không có nhân viên thu tiền, không gán ngẫu nhiên lễ tân.
    public Guid? ReceptionistId { get; set; }

    // CODE CŨ: Guid MemberMembershipId bắt buộc nên không lập được hóa đơn lớp.
    public Guid? MemberMembershipId { get; set; } // FIX B53
    public Guid? ClassBookingId { get; set; }
    public ClassBooking? ClassBooking { get; set; }
    public string Kind { get; set; } = "Membership";
    public decimal OriginalAmount { get; set; }
    public int DiscountPercent { get; set; }
    public decimal CreditAmount { get; set; }
    public Guid? UpgradeFromMembershipId { get; set; }
    public Membership? UpgradeFromMembership { get; set; }
    public DateTime? UpgradeSourceEndDate { get; set; }
    public string? FromPackageName { get; set; }
    public bool WasExpired { get; set; }
    public DateTimeOffset? RefundRequestedAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public Guid? RefundConfirmedByAccountId { get; set; }
    public Account? RefundConfirmedByAccount { get; set; }
    public string? RefundReference { get; set; }

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

    // CODE CŨ: Membership bắt buộc cho mọi Invoice.
    public Membership? Membership { get; set; } // FIX B53
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
