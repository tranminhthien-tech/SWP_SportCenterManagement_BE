namespace SWP_SportCenter.Repository.Enum;

public enum MembershipStatus
{
    Active = 1,
    Inactive = 2,
    Expired = 3,
    // FIX B10: trạng thái hủy hợp lệ, giữ nguyên mã enum cũ.
    Cancelled = 4
}
