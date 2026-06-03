using Project.Domain.Enums;

namespace Project.BLL.DTOs.EvaluationItems;

public class UpdateEvaluationItemRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
    public AutoCalcType AutoCalcType { get; set; } = AutoCalcType.None;
    public int DisplayOrder { get; set; }
    public decimal? AbsenceMaxScore { get; set; }
}
