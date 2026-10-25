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
        
        public Guid MemberMembershipId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        // CODE CŨ: public DateTimeOffset PaymentDate { get; set; }
        public DateTimeOffset? PaymentDate { get; set; } // FIX B39
        public InvoiceStatus Status { get; set; }
    }
}
