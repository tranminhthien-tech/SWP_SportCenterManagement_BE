using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Invoice;

public class Response
{
    public class InvoiceResponse
    {
        public Guid Id { get; set; }
        
        public Guid ReceptionistId { get; set; }
        public string ReceptionistName { get; set; } = string.Empty;
        
        public Guid MemberMembershipId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public DateTimeOffset PaymentDate { get; set; }
        public InvoiceStatus Status { get; set; }
    }
}