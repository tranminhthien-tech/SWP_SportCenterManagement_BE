namespace SWP_SportCenter.Service.TrainingPlan;

public class Response
{
    public class TrainingPlanResponse
    {
        public Guid TrainingPlanId { get; set; }

        public Guid CoachId { get; set; }

        public Guid MemberId { get; set; }

        public Guid ClassId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentDescription { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}