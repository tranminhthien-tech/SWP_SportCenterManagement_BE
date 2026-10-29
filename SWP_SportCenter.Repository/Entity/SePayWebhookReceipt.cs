namespace SWP_SportCenter.Repository.Entity;

// FIX SEPAY: sổ nhận webhook độc lập, giữ cả giao dịch cần đối soát; một Id SePay chỉ ghi một lần.
public sealed class SePayWebhookReceipt
{
    public long Id { get; set; }
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal TransferAmount { get; set; }
    public string AccountNumber { get; set; } = "";
    public string? SubAccount { get; set; }
    public string? Gateway { get; set; }
    public string? BankReferenceCode { get; set; }
    public string? BankTransactionDate { get; set; }
    public string? Content { get; set; }
    public string PayloadHash { get; set; } = "";
    public string Outcome { get; set; } = "";
    public DateTimeOffset ReceivedAt { get; set; }
}
