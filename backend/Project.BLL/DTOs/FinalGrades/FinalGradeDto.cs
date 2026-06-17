using Project.Domain.Enums;

namespace Project.BLL.DTOs.FinalGrades;

public class FinalGradeDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public AcademicTerm Term { get; set; }
    public decimal PeriodAvgScore { get; set; }
    public decimal Assessment1Score { get; set; }
    public decimal Assessment2Score { get; set; }
    public decimal WrittenTotal { get; set; }
    public decimal FinalExamScore { get; set; }
    public decimal Total { get; set; }
    public decimal MaxTotal { get; set; }
    public bool IsPublished { get; set; }
    public bool IsComplete { get; set; }
}
