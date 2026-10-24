using System;
using System.Threading.Tasks;

namespace SWP_SportCenter.Service.Payment;

public interface IPaymentService
{
    Task<Response.CheckoutResponse> CheckoutAsync(Request.CheckoutRequest request);
    Task<Response.PaymentDetailResponse> GetPaymentAsync(Guid paymentId);
    Task<Response.PaymentDetailResponse> ConfirmPaymentAsync(Guid paymentId);
}
