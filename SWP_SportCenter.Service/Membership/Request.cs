using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.Membership;

public class Request
{
    public class MembershipRequest
    {
        public Guid MemberId { get; set; }

        public Guid PackageId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public MembershipStatus Status { get; set; }
    }
}