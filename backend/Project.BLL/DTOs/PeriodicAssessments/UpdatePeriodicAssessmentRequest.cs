namespace Project.BLL.DTOs.PeriodicAssessments;

public class UpdatePeriodicAssessmentRequest
{
    public int AssessmentId { get; set; }
    public decimal Score { get; set; }
    public DateOnly? AssessmentDate { get; set; }
}
