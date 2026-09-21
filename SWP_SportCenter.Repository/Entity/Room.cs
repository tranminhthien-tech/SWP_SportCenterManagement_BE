using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Room: BaseEntity, IAuditableEntity
{
    public string RoomName { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public RoomStatus Status { get; set; }

    // Navigation

    public ICollection<ClassSession> ClassSessions { get; set; }
        = new List<ClassSession>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}