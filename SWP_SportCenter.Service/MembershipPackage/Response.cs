using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.MembershipPackage;

public class Response
{
    public class MembershipPackageResponse
    {
        public Guid MembershipPackageId { get; set; }

        public string PackageName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DurationDays { get; set; }

        public decimal Price { get; set; }

        public MembershipStatus Status { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}