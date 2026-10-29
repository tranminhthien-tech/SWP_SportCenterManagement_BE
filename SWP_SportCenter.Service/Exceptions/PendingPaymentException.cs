namespace SWP_SportCenter.Service.Exceptions;
// FIX B53 B55: client tiếp tục đơn của CHÍNH mình, không tạo nhiều hold.
public sealed class PendingPaymentException(Guid paymentId) : Exception("Đang có đơn chờ thanh toán. Hãy tiếp tục hoặc hủy đơn cũ.")
{ public Guid PaymentId { get; } = paymentId; }
