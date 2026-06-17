using Project.Domain.Enums;

namespace Project.BLL.DTOs.PeriodicAssessments;

public class PeriodicAssessmentDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public PeriodicAssessmentType AssessmentType { get; set; }
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public DateOnly? AssessmentDate { get; set; }
    public AcademicTerm? Term { get; set; }
}
