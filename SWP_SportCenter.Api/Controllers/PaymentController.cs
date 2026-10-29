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
    // FIX B53 B55: giữ SePay, không thêm route/khóa MoMo.
    [AllowAnonymous, HttpGet("options")]
    public IActionResult Options([FromServices] SWP_SportCenter.Service.Sepay.ISepayService gateway)
        => gateway.IsEnabled ? Ok(new { provider = "SePay", methods = new[] { 2 }, holdMinutes = 15, upgrade = true })
            : StatusCode(503, new { message = "SePay chưa được bật." });
    [Authorize(Policy = JwtExtensions.MemberPolicy), HttpPost("class-checkout")]
    public async Task<IActionResult> ClassCheckout(Request.ClassCheckoutRequest request, [FromServices] ClassCommerceService commerce)
        => Ok(await commerce.ClassCheckoutAsync(request));
    [Authorize(Policy = JwtExtensions.MemberPolicy), HttpPost("upgrade-checkout")]
    public async Task<IActionResult> Upgrade(Request.UpgradeCheckoutRequest request, [FromServices] ClassCommerceService commerce)
        => Ok(await commerce.UpgradeAsync(request));
    [Authorize(Policy = JwtExtensions.MemberPolicy), HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromServices] ClassCommerceService commerce)
    { await commerce.CancelPaymentAsync(id); return Ok(new { message = "Đã hủy đơn và trả chỗ nếu có." }); }
    [Authorize(Policy = JwtExtensions.ReceptionistPolicy), HttpPost("{id:guid}/refund-confirm")]
    public async Task<IActionResult> Refund(Guid id, Request.ConfirmRefundRequest request, [FromServices] ClassCommerceService commerce)
    { await commerce.ConfirmRefundAsync(id, request); return Ok(new { message = "Đã ghi nhận xác nhận hoàn tiền thực tế." }); }
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
    // CODE CŨ: ManagerOrReceptionistPolicy.
    [Authorize(Policy = JwtExtensions.ReceptionistPolicy)] // FIX B58: xác nhận thu chỉ cho lễ tân.
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
        // CODE CŨ: thông báo chưa tích hợp cổng thật.
        return StatusCode(410, new { message = "Mock gateway đã đóng. Chuyển khoản dùng QR SePay khi đã cấu hình; xác nhận thủ công chỉ dành cho staff." }); // FIX SEPAY
    }
}
