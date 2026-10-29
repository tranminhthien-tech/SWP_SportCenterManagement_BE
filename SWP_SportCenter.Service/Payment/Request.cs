using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.Payment;

public class Request
{
    public class ClassCheckoutRequest
    {
        public Guid ClassId { get; set; }
        [Range(2,2)] public int PaymentMethod { get; set; } = 2;
        public string? ReturnUrl { get; set; }
    }
    public class UpgradeCheckoutRequest
    {
        public Guid MembershipId { get; set; }
        public Guid MembershipPackageId { get; set; }
        [Range(2,2)] public int PaymentMethod { get; set; } = 2;
        public string? ReturnUrl { get; set; }
    }
    public class ConfirmRefundRequest
    {
        [Required, MaxLength(200)] public string Reference { get; set; } = "";
    }
    public class CheckoutRequest
    {
        [Required(ErrorMessage = "Vui lòng chọn gói tập")]
        public Guid MembershipPackageId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        // 2: chuyển khoản (BankTransfer), 3: thẻ (Card)
        public int PaymentMethod { get; set; } = 2;

        public string? ReturnUrl { get; set; }
    }
}
