using System;

namespace SWP_SportCenter.Service.Payment;

public class Response
{
    public class CheckoutResponse
    {
        public string Kind { get; set; } = "Membership"; // FIX B53 B55
        public string Provider { get; set; } = "SePay";
        public int PaymentMethod { get; set; } = 2;
        public string? PayUrl { get; set; }
        public Guid? BookingId { get; set; }
        public decimal OriginalAmount { get; set; }
        public int DiscountPercent { get; set; }
        public decimal CreditAmount { get; set; }
        public string? FromPackageName { get; set; }
        // FIX SEPAY: chỉ dẫn công khai cho FE, TUYỆT ĐỐI không trả WebhookApiKey.
        public string? QrCodeUrl { get; set; }
        public string? PaymentReference { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? BankBin { get; set; }
        public string? BankAccountNumber { get; set; }
        public Guid PaymentId { get; set; }
        // CODE CŨ: Guid MembershipId (hóa đơn lớp không có thẻ).
        public Guid? MembershipId { get; set; } // FIX B53
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; }
        public string? PaymentUrl { get; set; }
    }

    public class PaymentDetailResponse
    {
        public string Kind { get; set; } = "Membership"; // FIX B53 B55 B56
        public string Provider { get; set; } = "SePay";
        public int PaymentMethod { get; set; }
        public string? PayUrl { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public Guid? BookingId { get; set; }
        public Guid? PackageId { get; set; }
        public decimal OriginalAmount { get; set; }
        public int DiscountPercent { get; set; }
        public decimal CreditAmount { get; set; }
        public string? FromPackageName { get; set; }
        public decimal? RefundAmount { get; set; }
        public bool RefundPending { get; set; }
        public string? RefundReference { get; set; }
        public string? QrCodeUrl { get; set; } // FIX SEPAY: null khi đã trả/hết hạn.
        public string? PaymentReference { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public string? BankBin { get; set; }
        public string? BankAccountNumber { get; set; }
        public Guid PaymentId { get; set; }
        public int Status { get; set; } // 1: Pending | 2: Paid | 3: Failed | 4: Cancelled
        public decimal Amount { get; set; }
        public string PackageName { get; set; } = string.Empty;
        // CODE CŨ: Guid MembershipId.
        public Guid? MembershipId { get; set; } // FIX B53
        public Guid InvoiceId { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
    }
}
