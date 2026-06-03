namespace Project.BLL.DTOs;

public class ClassWithStudentsDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TemplateId { get; set; }
    public string Teacher { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public int GradeLevelId { get; set; }
    public string GradeLevelName { get; set; } = string.Empty;
    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public List<StudentItemDto> Students { get; set; } = new();
}
