using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Membership;

public class Response
{
    public class MembershipResponse
    {
        public Guid MembershipId { get; set; }

        public Guid MemberId { get; set; }

        public Guid PackageId { get; set; }

        public string PackageName { get; set; } = string.Empty;

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public MembershipStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}