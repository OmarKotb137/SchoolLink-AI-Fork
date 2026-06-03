namespace Project.BLL.DTOs.StudentEvaluations;

public class StudentEvaluationDto
{
    public int Id { get; set; }
    public int EnrollmentId { get; set; }
    public int EvaluationItemId { get; set; }
    public int PeriodId { get; set; }
    public decimal? Score { get; set; }
    public int EnteredById { get; set; }
    public DateTime EnteredAt { get; set; }
    public string? ItemName { get; set; }
    public decimal MaxScore { get; set; }
}
