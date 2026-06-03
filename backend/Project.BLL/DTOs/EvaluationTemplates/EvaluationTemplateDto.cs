using Project.Domain.Enums;

namespace Project.BLL.DTOs.EvaluationTemplates;

public class EvaluationTemplateDto
{
    public int Id { get; set; }
    public int GradeLevelId { get; set; }
    public int SubjectId { get; set; }
    public int AcademicYearId { get; set; }
    public string Name { get; set; } = string.Empty;
    public EvaluationCalculationType CalculationType { get; set; }
    public bool IsActive { get; set; }
    public string? GradeLevelName { get; set; }
    public string? SubjectName { get; set; }
    public string? AcademicYearName { get; set; }
}
