using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Service.Sepay;
using SepayRequest = SWP_SportCenter.Service.Sepay.Request;

namespace SWP_SportCenter.Controllers;

[ApiController, Route("api/sepay")]
public sealed class SePayController(ISepayService service) : ControllerBase
{
    // FIX SEPAY: SePay không có JWT người dùng, nhưng API Key là BẮT BUỘC.
    // THAY_THONG_TIN_SEPAY: URL dashboard = https://DOMAIN_API_CUA_BAN/api/sepay/webhook
    [HttpPost("webhook"), AllowAnonymous, Consumes("application/json"), RequestSizeLimit(16384)]
    public async Task<IActionResult> Webhook([FromBody] SepayRequest.SepayWebhookDto data)
    {
        if (!service.VerifyWebhookAuthorization(Request.Headers.Authorization.ToString()))
            return Unauthorized(new { success = false, message = "Webhook SePay không được xác thực." });
        return Ok(new { success = await service.ProcessSePayWebhook(data) });
    }
}
