using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.Sepay;

public class Request
{
    // FIX SEPAY: chuyển DTO Juratifact sang SWP, bổ sung tài khoản nhận và mã nội dung.
    public class SepayWebhookDto
    {
        [Range(typeof(long), "1", "9223372036854775807")]
        public long Id { get; set; }
        [MaxLength(2000)] public string? Content { get; set; }
        [Range(typeof(decimal), "1", "999999999999999")]
        public decimal TransferAmount { get; set; }
        [MaxLength(100)] public string? ReferenceCode { get; set; } // Mã ngân hàng, KHÔNG phải mã hóa đơn SWP.
        [MaxLength(64)] public string? TransactionDate { get; set; }
        [MaxLength(100)] public string? Gateway { get; set; }
        [Required, MaxLength(10)] public string TransferType { get; set; } = "";
        [Required, MaxLength(64)] public string AccountNumber { get; set; } = "";
        [MaxLength(64)] public string? SubAccount { get; set; }
        [MaxLength(100)] public string? Code { get; set; }
    }
}
