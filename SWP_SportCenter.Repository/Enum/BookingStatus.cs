namespace SWP_SportCenter.Repository.Enum;

public enum BookingStatus
{
    PendingPayment = 4, // FIX B53: giữ chỗ 15 phút, chưa được điểm danh.
    Booked = 1,
    Attended = 2,
    Cancelled = 3
}
