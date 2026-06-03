using Project.Domain.Enums;

namespace Project.BLL.DTOs.EvaluationItems;

public class EvaluationItemDto
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public ItemType ItemType { get; set; }
    public AutoCalcType AutoCalcType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsVisible { get; set; }
}
