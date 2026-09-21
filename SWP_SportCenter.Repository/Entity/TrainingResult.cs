using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class TrainingResult: BaseEntity, IAuditableEntity
{
    public Guid PlanId { get; set; }

    public Guid MemberId { get; set; }

    public Guid CoachId { get; set; }

    public float PerformanceScore { get; set; }

    public string CoachComment { get; set; } = string.Empty;

    public DateTime EvaluationDate { get; set; }

    // Navigation

    public TrainingPlan Plan { get; set; } = null!;

    public Member Member { get; set; } = null!;

    public Coach Coach { get; set; } = null!;
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}