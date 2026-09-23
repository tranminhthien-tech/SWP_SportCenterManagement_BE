using System;
using System.ComponentModel.DataAnnotations;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Invoice;

public class Request
{
    public class CreateInvoiceRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn Lễ tân (Receptionist)")]
        public Guid ReceptionistId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Gói đăng ký của học viên")]
        public Guid MemberMembershipId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Số tiền phải lớn hơn 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; }
        
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
    }

    public class UpdateInvoiceRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public PaymentMethod PaymentMethod { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái hóa đơn")]
        public InvoiceStatus Status { get; set; }
    }
}