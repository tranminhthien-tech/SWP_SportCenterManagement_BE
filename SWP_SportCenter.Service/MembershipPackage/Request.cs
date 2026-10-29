using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Service.MembershipPackage;

public class Request
{
    public class MembershipPackageRequest
    {
        [System.ComponentModel.DataAnnotations.Range(0, 100)] public int DiscountPercent { get; set; } // FIX B53
        [System.ComponentModel.DataAnnotations.MaxLength(2000)] public string Benefits { get; set; } = "";
        // FIX B46: [] = mọi môn; bỏ trường khi PUT thì giữ phạm vi cũ.
        public List<Guid>? CategoryIds { get; set; }
        public string PackageName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DurationDays { get; set; }

        public decimal Price { get; set; }

        public MembershipStatus Status { get; set; }
    }
}
