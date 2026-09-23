using System;
using System.ComponentModel.DataAnnotations;

namespace SWP_SportCenter.Service.ClassBooking;

public class Request
{
    public class CreateBookingRequest
    {
        [Required(ErrorMessage = "Vui lòng cung cấp ID của Học viên")]
        public Guid MemberId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Lớp học muốn đăng ký")]
        public Guid ClassId { get; set; }
    }
}