namespace Project.BLL.DTOs.StudentEvaluations;

public class UpdateEvaluationRequest
{
    public int EvaluationId { get; set; }
    public decimal? NewScore { get; set; }
    public int UpdatedById { get; set; }
}
