using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.ClassBooking;

public class Response
{
    public class BookingResponse
    {
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