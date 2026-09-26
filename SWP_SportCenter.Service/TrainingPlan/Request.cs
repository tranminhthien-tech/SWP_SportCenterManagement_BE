namespace SWP_SportCenter.Service.TrainingPlan;

public class Request
{
    public class TrainingPlanRequest
    {
        public Guid CoachId { get; set; }

        public Guid MemberId { get; set; }

        public Guid ClassId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string ContentDescription { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }
    }
}