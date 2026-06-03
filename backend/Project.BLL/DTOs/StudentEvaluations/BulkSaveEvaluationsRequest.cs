namespace Project.BLL.DTOs.StudentEvaluations;

public class BulkSaveEvaluationsRequest
{
    public int EnteredById { get; set; }
    public List<EvaluationEntry> Entries { get; set; } = new();
}

public class EvaluationEntry
{
    public int? EvaluationId { get; set; }
    public int EnrollmentId { get; set; }
    public int EvaluationItemId { get; set; }
    public int PeriodId { get; set; }
    public decimal? Score { get; set; }
}
