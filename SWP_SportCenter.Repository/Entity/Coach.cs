using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class Coach: BaseEntity, IAuditableEntity
{
    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Avatar { get; set; } = string.Empty;

    public string Specialization { get; set; } = string.Empty;

    public int ExperienceYears { get; set; }

    // Navigation
    
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public ICollection<Class> Classes { get; set; }
        = new List<Class>();

    public ICollection<TrainingPlan> TrainingPlans { get; set; }
        = new List<TrainingPlan>();

    public ICollection<TrainingResult> TrainingResults { get; set; }
        = new List<TrainingResult>();
    
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}