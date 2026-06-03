namespace Project.BLL.DTOs.StudentEvaluations;

public class BulkRecordEvaluationRequest
{
    public List<SingleRecordRequest> Evaluations { get; set; } = new();
    public int EnteredById { get; set; }
}

public class SingleRecordRequest
{
    public int EnrollmentId { get; set; }
    public int EvaluationItemId { get; set; }
    public int PeriodId { get; set; }
    public decimal? Score { get; set; }
}
