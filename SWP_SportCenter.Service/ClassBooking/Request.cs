using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.ClassBooking;

public class Request
{
    public class CreateBookingRequest
    {
        [Range(1, 3)] public int PaymentMethod { get; set; } = 1; // FIX B53: thu tại quầy.
        [Required(ErrorMessage = "Vui lòng cung cấp ID của Học viên")]
        public Guid MemberId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Lớp học muốn đăng ký")]
        public Guid ClassId { get; set; }
    }
}
