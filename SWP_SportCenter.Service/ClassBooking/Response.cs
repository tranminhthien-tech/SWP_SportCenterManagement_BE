using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.ClassBooking;

public class Response
{
    public class CancellationResponse
    {
        public Guid BookingId { get; set; }
        public BookingStatus Status { get; set; }
        public bool Refunded { get; set; }
        public decimal RefundAmount { get; set; }
        public bool RefundPending { get; set; }
        public decimal RequestedRefundAmount { get; set; }
    }
    public class BookingResponse
    {
        public decimal OriginalPrice { get; set; } // FIX B53 B56
        public int DiscountPercent { get; set; }
        public decimal Price { get; set; }
        public Guid? InvoiceId { get; set; }
        public int? PaymentMethod { get; set; }
        public DateTimeOffset? PaymentExpiresAt { get; set; }
        public DateTimeOffset? CancelledAt { get; set; }
        public decimal? RefundAmount { get; set; }
        public bool RefundPending { get; set; }
        // FIX B45: thẻ dùng để xét quyền lúc đặt, không thay đổi DB booking cũ.
        public Guid? EligibleMembershipId { get; set; }
        public DateTime? MembershipEndDate { get; set; }
        public Guid Id { get; set; }
        
        public Guid MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;

        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        
        // Bổ sung thêm ngày bắt đầu lớp học để hiển thị cho Member dễ theo dõi
        public DateTime ClassStartDate { get; set; } 

        public DateTimeOffset BookingDate { get; set; }
        public BookingStatus Status { get; set; }
    }
}
