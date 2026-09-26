using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class Member: BaseEntity, IAuditableEntity
{
    


    public string FullName { get; set; } = string.Empty;

    public DateTime Dob { get; set; }

    public string Gender { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string TrainingGoal { get; set; } = string.Empty;

    // Navigation
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public ICollection<Membership> Memberships { get; set; }
        = new List<Membership>();

    public ICollection<ClassBooking> ClassBookings { get; set; }
        = new List<ClassBooking>();

    public ICollection<Attendance> Attendances { get; set; }
        = new List<Attendance>();

    public ICollection<TrainingPlan> TrainingPlans { get; set; }
        = new List<TrainingPlan>();

    public ICollection<TrainingResult> TrainingResults { get; set; }
        = new List<TrainingResult>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}