using Project.Domain.Enums;

namespace Project.BLL.DTOs.EvaluationTemplates;

public class UpdateEvaluationTemplateRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EvaluationCalculationType CalculationType { get; set; }
    public bool IsActive { get; set; }
    public int Weeks { get; set; } = 12;
}
