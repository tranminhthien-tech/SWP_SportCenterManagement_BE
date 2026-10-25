using System;

namespace SWP_SportCenter.Service.Payment;

public class Response
{
    public class CheckoutResponse
    {
        public Guid PaymentId { get; set; }
        public Guid MembershipId { get; set; }
        public Guid InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public int Status { get; set; }
        public string? PaymentUrl { get; set; }
    }

    public class PaymentDetailResponse
    {
        public Guid PaymentId { get; set; }
        public int Status { get; set; } // 1: Pending | 2: Paid | 3: Failed | 4: Cancelled
        public decimal Amount { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public Guid MembershipId { get; set; }
        public Guid InvoiceId { get; set; }
        public DateTimeOffset? PaidAt { get; set; }
    }
}
