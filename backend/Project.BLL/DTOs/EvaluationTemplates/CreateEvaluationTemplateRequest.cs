using Project.Domain.Enums;

namespace Project.BLL.DTOs.EvaluationTemplates;

public class CreateEvaluationTemplateRequest
{
    public int GradeLevelId { get; set; }
    public int SubjectId { get; set; }
    public int AcademicYearId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EvaluationCalculationType CalculationType { get; set; }
    public int Weeks { get; set; } = 12;
    public AcademicTerm? Term { get; set; }
}
