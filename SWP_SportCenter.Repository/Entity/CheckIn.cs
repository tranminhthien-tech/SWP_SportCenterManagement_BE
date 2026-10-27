using SWP_SportCenter.Repository.Abstraction;
namespace SWP_SportCenter.Repository.Entity;

// FIX B47: lượt vào cửa độc lập Attendance của buổi học.
public class CheckIn : BaseEntity, IAuditableEntity
{
    public Guid MemberId { get; set; }
    public Member Member { get; set; } = null!;
    public Guid MembershipId { get; set; }
    public Membership Membership { get; set; } = null!;
    public Guid? ReceptionistId { get; set; }
    public Receptionist? Receptionist { get; set; }
    public Guid RecordedByAccountId { get; set; }
    public Account RecordedByAccount { get; set; } = null!;
    public DateTimeOffset CheckInTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
