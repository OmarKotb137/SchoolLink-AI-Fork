namespace Project.BLL.DTOs.ClassAnalysis;

public class ClassAnalysisOverviewDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public int TotalStudents { get; set; }
    public double ClassAverage { get; set; }
    public double ClassAverageChange { get; set; }
    public double MaxScore { get; set; } = 100;
    public int TopStudentsCount { get; set; }
    public int AtRiskStudentsCount { get; set; }
    public double AttendanceRate { get; set; }
}

public class SubjectPerformanceDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public double ClassAverage { get; set; }
    public double SchoolAverage { get; set; }
    public double MaxScore { get; set; } = 100;
    public double Difference => ClassAverage - SchoolAverage;
}

public class AttendanceTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int MonthNumber { get; set; }
    public int Year { get; set; }
    public double AttendanceRate { get; set; }
    public double AbsenceRate { get; set; }
    public int TotalSchoolDays { get; set; }
    public int AbsenceDays { get; set; }
}

public class TopStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public double MaxScore { get; set; } = 100;
    public int Rank { get; set; }
    public string? PhotoUrl { get; set; }
}

public class AtRiskStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public double MaxScore { get; set; } = 100;
    public double AttendanceRate { get; set; }
    public List<string> WeakSubjects { get; set; } = new();
    public string Severity { get; set; } = "warning"; // warning, danger, critical
}

public class WeaknessDto
{
    public string SkillName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium"; // safe, low, medium, critical
    public double AverageScore { get; set; }
    public double MaxScore { get; set; } = 100;
}

public class ClassStudentListDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public double AttendanceRate { get; set; }
    public int AbsenceCount { get; set; }
    public string Status { get; set; } = "active"; // active, at-risk, excellent
}

public class ClassAnalysisFullDto
{
    public ClassAnalysisOverviewDto Overview { get; set; } = null!;
    public List<SubjectPerformanceDto> SubjectPerformance { get; set; } = new();
    public List<AttendanceTrendDto> AttendanceTrends { get; set; } = new();
    public List<TopStudentDto> TopStudents { get; set; } = new();
    public List<AtRiskStudentDto> AtRiskStudents { get; set; } = new();
    public List<WeaknessDto> WeaknessAnalysis { get; set; } = new();
    public List<ClassStudentListDto> Students { get; set; } = new();
}
