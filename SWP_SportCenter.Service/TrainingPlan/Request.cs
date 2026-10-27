namespace SWP_SportCenter.Service.TrainingPlan;

public class Request
{
    // FIX B49: kế hoạch chung không nhận MemberId/CoachId từ client.
    public class ClassTrainingPlanRequest
    {
        [System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.MaxLength(150)]
        public string Title { get; set; } = string.Empty;
        [System.ComponentModel.DataAnnotations.MaxLength(4000)]
        public string ContentDescription { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
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
