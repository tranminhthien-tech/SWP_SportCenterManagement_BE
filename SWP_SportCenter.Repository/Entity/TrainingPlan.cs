using SWP_SportCenter.Repository.Abstraction;

namespace SWP_SportCenter.Repository.Entity;

public class TrainingPlan: BaseEntity, IAuditableEntity
{
    public Guid CoachId { get; set; }

    public Guid MemberId { get; set; }

    public Guid ClassId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ContentDescription { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    // Navigation

    public Coach Coach { get; set; } = null!;

    public Member Member { get; set; } = null!;

    public Class Class { get; set; } = null!;

    public ICollection<TrainingResult> TrainingResults { get; set; }
        = new List<TrainingResult>();
    
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}