namespace Project.BLL.DTOs.EvaluationItems;

public class UpdateEvaluationItemRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public int DisplayOrder { get; set; }
}
