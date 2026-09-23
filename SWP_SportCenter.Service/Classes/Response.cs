using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Classes;

public class Response
{
    public class ClassResponse
    {
        public Guid Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty; 

        public Guid CoachId { get; set; }
        public string CoachName { get; set; } = string.Empty; 

        public int MaxCapacity { get; set; }
        public DateTime StartDate { get; set; } // Trả về DateTime
        public DateTime EndDate { get; set; }   // Trả về DateTime
        public ClassStatus Status { get; set; }
        
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class ClassMemberResponse
    {
        public Guid MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public BookingStatus BookingStatus { get; set; }
        public DateTimeOffset BookingDate { get; set; } // GIỮ NGUYÊN DateTimeOffset
    }
}