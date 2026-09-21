using SWP_SportCenter.Repository.Abstraction;
using SWP_SportCenter.Repository.Enum;

namespace SWP_SportCenter.Repository.Entity;

public class Class: BaseEntity, IAuditableEntity
{
    public string ClassName { get; set; } = string.Empty;

    public Guid CategoryId { get; set; }

    public Guid CoachId { get; set; }

    public int MaxCapacity { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public ClassStatus Status { get; set; }

    // Navigation

    public SportCategory Category { get; set; } = null!;

    public Coach Coach { get; set; } = null!;

    public ICollection<ClassSession> ClassSessions { get; set; }
        = new List<ClassSession>();

    public ICollection<ClassBooking> ClassBookings { get; set; }
        = new List<ClassBooking>();

    public ICollection<TrainingPlan> TrainingPlans { get; set; }
        = new List<TrainingPlan>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}