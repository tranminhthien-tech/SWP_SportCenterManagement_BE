namespace SWP_SportCenter.Service.TrainingResult;

public class Request
{
    public class TrainingResultRequest
    {
        public Guid PlanId { get; set; }

        public Guid MemberId { get; set; }

        public Guid CoachId { get; set; }

        // FIX B13: validate điểm 0..10 trước khi vào Service.
        [System.ComponentModel.DataAnnotations.Range(0, 10)]
        public float PerformanceScore { get; set; }

        public string CoachComment { get; set; } = string.Empty;

        public DateTime EvaluationDate { get; set; }
    }
}
