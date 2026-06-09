namespace Project.BLL.DTOs.ChildProgress;

public class ChildProgressItemDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public double AvgScore { get; set; }
    public double AttendancePercentage { get; set; }
    public List<AssignmentProgressDto> Assignments { get; set; } = new();
    public List<ExamProgressDto> Exams { get; set; } = new();
}

public class AssignmentProgressDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Deadline { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? Score { get; set; }
    public double MaxScore { get; set; }
}

public class ExamProgressDto
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public double? Score { get; set; }
    public double MaxScore { get; set; }
}
