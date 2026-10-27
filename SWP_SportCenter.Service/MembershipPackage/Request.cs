using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.MembershipPackage;

public class Request
{
    public class MembershipPackageRequest
    {
        // FIX B46: [] = mọi môn; bỏ trường khi PUT thì giữ phạm vi cũ.
        public List<Guid>? CategoryIds { get; set; }
        public string PackageName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DurationDays { get; set; }

        public decimal Price { get; set; }

        public MembershipStatus Status { get; set; }
    }
}
