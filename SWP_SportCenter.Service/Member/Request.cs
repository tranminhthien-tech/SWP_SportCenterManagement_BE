using System.ComponentModel.DataAnnotations;
namespace SWP_SportCenter.Service.Member;

public class Request
{
    public class MemberRequest
    {
        public Guid AccountId { get; set; }
        // FIX B17: validate từng trường trước khi Service dùng Trim.
        [Required, MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        [SWP_SportCenter.Service.Validation.DateOfBirth]
        public DateTime Dob { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        // CODE CŨ: public string? Phone { get; set; }
        // FIX B17: bắt buộc số Việt Nam 10 chữ số.
        [Required, RegularExpression(@"^0[0-9]{9}$")]
        public string Phone { get; set; } = string.Empty;

        // CODE CŨ: public string? Email { get; set; }
        // FIX B17: không Trim null, email đúng định dạng.
        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Avatar { get; set; }

        [MaxLength(500)]
        public string? TrainingGoal { get; set; }
    }
}
