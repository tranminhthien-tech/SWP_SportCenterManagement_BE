using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SWP_SportCenter.Extensions;
using SWP_SportCenter.Service.Payment;

namespace SWP_SportCenter.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    // POST: /api/payments/checkout
    // FIX B38: Đăng ký mua và thanh toán gói tập trực tuyến
    [Authorize(Policy = JwtExtensions.MemberPolicy)]
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] Request.CheckoutRequest request)
    {
        var result = await _paymentService.CheckoutAsync(request);
        return Ok(result);
    }

    // GET: /api/payments/{paymentId}
    // FIX B38: Xem chi tiết trạng thái giao dịch thanh toán
    [Authorize]
    [HttpGet("{paymentId:guid}")]
    public async Task<IActionResult> GetById(Guid paymentId)
    {
        var result = await _paymentService.GetPaymentAsync(paymentId);
        return Ok(result);
    }

    // POST: /api/payments/{paymentId}/confirm
    // FIX B38: Cổng thanh toán hoặc Webhook xác nhận giao dịch thành công
    [AllowAnonymous]
    [HttpPost("{paymentId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid paymentId)
    {
        var result = await _paymentService.ConfirmPaymentAsync(paymentId);
        return Ok(result);
    }

    // GET: /api/payments/mock-gateway
    // FIX B38: Giả lập cổng thanh toán trực tuyến: tự động xác nhận và redirect về returnUrl
    [AllowAnonymous]
    [HttpGet("mock-gateway")]
    public async Task<IActionResult> MockGateway([FromQuery] Guid paymentId, [FromQuery] string returnUrl)
    {
        await _paymentService.ConfirmPaymentAsync(paymentId);
        var delimiter = returnUrl.Contains("?") ? "&" : "?";
        return Redirect($"{returnUrl}{delimiter}paymentId={paymentId}&status=2");
    }
}
