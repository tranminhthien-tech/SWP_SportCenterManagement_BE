namespace SWP_SportCenter.Repository.Enum;

public enum InvoiceStatus
{
    Refunded = 4, // FIX B56: chỉ sau khi lễ tân xác nhận đã thực trả.
    Pending = 1,
    Paid = 2,
    Cancelled = 3
}
