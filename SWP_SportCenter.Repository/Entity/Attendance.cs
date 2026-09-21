using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Attendance:BaseEntity, IAuditableEntity
{
    public Guid SessionId { get; set; }

    public Guid MemberId { get; set; }

    public Guid RecordedBy { get; set; }

    public DateTimeOffset CheckInTime { get; set; }

    public AttendanceStatus Status { get; set; }

    // Navigation

    public ClassSession Session { get; set; } = null!;

    public Member Member { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}