using Project.Domain.Enums;

namespace Project.BLL.DTOs.PeriodicAssessments;

public class RecordPeriodicAssessmentRequest
{
    public int EnrollmentId { get; set; }
    public PeriodicAssessmentType AssessmentType { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateOnly? AssessmentDate { get; set; }
}
