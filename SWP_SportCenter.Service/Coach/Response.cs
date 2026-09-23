using System;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Coach;

public class Response
{
    public class CoachResponse
    {
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Specialization { get; set; }
        public int ExperienceYears { get; set; }
    }

    // DTO trả về cho API xem danh sách lớp HLV đang dạy
    public class CoachClassResponse
    {
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int MaxCapacity { get; set; }
        public int CurrentEnrolled { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ClassStatus Status { get; set; }
    }

    // DTO trả về cho API xem danh sách học viên của HLV
    public class CoachMemberResponse
    {
        public Guid MemberId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public Guid ClassId { get; set; }
        public string ClassName { get; set; } = string.Empty;
    }
}