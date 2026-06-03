namespace Project.BLL.DTOs.StudentEvaluations;

public class RecordEvaluationRequest
{
    public int EnrollmentId { get; set; }
    public int EvaluationItemId { get; set; }
    public int PeriodId { get; set; }
    public decimal? Score { get; set; }
    public int EnteredById { get; set; }
}
