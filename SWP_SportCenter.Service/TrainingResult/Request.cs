namespace SWP_SportCenter.Service.TrainingResult;

public class Request
{
    public class TrainingResultRequest
    {
        public Guid PlanId { get; set; }

        public Guid MemberId { get; set; }

        public Guid CoachId { get; set; }

        public float PerformanceScore { get; set; }

        public string CoachComment { get; set; } = string.Empty;

        public DateTime EvaluationDate { get; set; }
    }
}