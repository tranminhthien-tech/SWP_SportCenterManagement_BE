using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class Member: BaseEntity, IAuditableEntity
{
    


    public string FullName { get; set; } = string.Empty;

    // CODE CŨ: public DateTime Dob { get; set; }
    // FIX B18: hồ sơ cũ chưa biết ngày sinh lưu null.
    public DateTime? Dob { get; set; }

    public string Gender { get; set; } = string.Empty;

    // CODE CŨ: public string Phone { get; set; } = string.Empty;
    // FIX B18: chưa biết phone thật thì null.
    public string? Phone { get; set; }
    
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
