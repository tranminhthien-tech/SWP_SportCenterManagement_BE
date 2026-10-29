using SWP_SportCenter.Repository.Entity;
namespace SWP_SportCenter.Service.Sepay;

public interface ISepayService
{
    bool IsEnabled { get; }
    bool VerifyWebhookAuthorization(string? authorization);
    Task<bool> ProcessSePayWebhook(Request.SepayWebhookDto data);
    Task<string> GenerateQrCode(decimal amount, string referenceCode);
    Task PrepareInvoiceAsync(Repository.Entity.Invoice invoice);
}
