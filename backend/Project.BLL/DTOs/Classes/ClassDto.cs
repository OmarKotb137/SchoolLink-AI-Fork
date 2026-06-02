namespace Project.BLL.DTOs;

public class ClassDto
{
    public int    Id               { get; set; }
    public string Name             { get; set; } = string.Empty;
    public int    GradeLevelId     { get; set; }
    public string GradeLevelName   { get; set; } = string.Empty;
    public int    AcademicYearId   { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
}
