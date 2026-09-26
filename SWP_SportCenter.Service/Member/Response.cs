namespace SWP_SportCenter.Service.Member;

public class Response
{
    public class MemberResponse
    {
        public Guid MemberId { get; set; }

        public Guid AccountId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public DateTime? Dob { get; set; }

        public string? Gender { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Avatar { get; set; }

        public string? TrainingGoal { get; set; }
    }
}