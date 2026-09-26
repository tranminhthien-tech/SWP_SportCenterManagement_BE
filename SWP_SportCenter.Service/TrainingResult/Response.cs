namespace SWP_SportCenter.Service.TrainingResult;

public class Response
{
    public class TrainingResultResponse
    {
        public Guid TrainingResultId { get; set; }

        public Guid PlanId { get; set; }

        public Guid MemberId { get; set; }

        public Guid CoachId { get; set; }

        public float PerformanceScore { get; set; }

        public string CoachComment { get; set; } = string.Empty;

        public DateTime EvaluationDate { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }
    }
}