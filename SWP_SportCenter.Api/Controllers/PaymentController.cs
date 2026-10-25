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
    // CODE CŨ: [AllowAnonymous]
    // FIX B39: đây là xác nhận thủ công nội bộ, không phải IPN cổng thật.
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpPost("{paymentId:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid paymentId)
    {
        var result = await _paymentService.ConfirmPaymentAsync(paymentId);
        return Ok(result);
    }

    // GET: /api/payments/mock-gateway
    // FIX B38: Giả lập cổng thanh toán trực tuyến: tự động xác nhận và redirect về returnUrl
    // CODE CŨ: [AllowAnonymous], GET tự ConfirmPaymentAsync rồi redirect URL bất kỳ.
    // FIX B39: GET không bao giờ ghi nhận trả tiền; đóng mock gateway cũ.
    [Authorize(Policy = JwtExtensions.ManagerOrReceptionistPolicy)]
    [HttpGet("mock-gateway")]
    public async Task<IActionResult> MockGateway([FromQuery] Guid paymentId, [FromQuery] string returnUrl)
    {
        /* CODE CŨ:
        await _paymentService.ConfirmPaymentAsync(paymentId);
        var delimiter = returnUrl.Contains("?") ? "&" : "?";
        return Redirect($"{returnUrl}{delimiter}paymentId={paymentId}&status=2");
        */
        return StatusCode(410, new { message = "Mock gateway đã đóng. Dùng xác nhận POST có quyền staff; chưa tích hợp cổng thật." }); // FIX B39
    }
}
