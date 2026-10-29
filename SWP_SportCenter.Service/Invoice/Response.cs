using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Invoice;

public class Response
{
    public class InvoiceResponse
    {
        public Guid Id { get; set; }
        
        // CODE CŨ: public Guid ReceptionistId { get; set; }
        public Guid? ReceptionistId { get; set; } // FIX B39
        public string ReceptionistName { get; set; } = string.Empty;
        
        // CODE CŨ: Guid MemberMembershipId bắt buộc.
        public Guid? MemberMembershipId { get; set; } // FIX B53
        public Guid? ClassBookingId { get; set; }
        public Guid? ClassId { get; set; }
        public string? ClassName { get; set; }
        public string Kind { get; set; } = "Membership";
        public decimal OriginalAmount { get; set; }
        public int DiscountPercent { get; set; }
        public decimal CreditAmount { get; set; }
        public bool RefundPending { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        // CODE CŨ: public DateTimeOffset PaymentDate { get; set; }
        public DateTimeOffset? PaymentDate { get; set; } // FIX B39
        public InvoiceStatus Status { get; set; }
    }
}
