namespace Project.BLL.DTOs.Dashboard;

public class TeacherDashboardDto
{
    public string UserName { get; set; } = string.Empty;
    public int TodayClassesCount { get; set; }
    public int TotalStudentsCount { get; set; }
    public int PendingSubmissionsCount { get; set; }
    public List<TeacherClassDto> Classes { get; set; } = new();
    public List<string> Tasks { get; set; } = new();
}

public class TeacherClassDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int ClassSubjectTeacherId { get; set; }
    public int StudentCount { get; set; }
}
