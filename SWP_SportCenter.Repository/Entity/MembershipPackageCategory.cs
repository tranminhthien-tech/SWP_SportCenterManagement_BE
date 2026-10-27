namespace SWP_SportCenter.Repository.Entity;

// FIX B46: không có dòng nối nào nghĩa là gói dùng cho mọi môn.
public class MembershipPackageCategory
{
    public Guid PackageId { get; set; }
    public MembershipPackage Package { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public SportCategory Category { get; set; } = null!;
}
