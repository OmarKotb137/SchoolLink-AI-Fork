using Project.Domain.Enums;

namespace Project.BLL.DTOs.ParentStudents;

public class ParentDashboardChildDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string? ClassName { get; set; }
    public string? GradeLevelName { get; set; }
    public bool IsActive { get; set; }
    public RelationshipType Relationship { get; set; }
}
