using Project.Domain.Enums;

namespace Project.BLL.DTOs.EvaluationItems;

public class CreateEvaluationItemRequest
{
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; } = 1;
    public ItemType ItemType { get; set; }
    public int DisplayOrder { get; set; }
}
