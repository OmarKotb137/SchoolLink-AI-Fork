using Project.BLL.DTOs.Common;

namespace Project.BLL.DTOs.ClassStudentsBrowser;

public class ClassStudentsBrowserResultDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public int FilteredStudentsCount { get; set; }
    public PagedResult<ClassStudentBrowserItemDto> Students { get; set; } = new();
}
