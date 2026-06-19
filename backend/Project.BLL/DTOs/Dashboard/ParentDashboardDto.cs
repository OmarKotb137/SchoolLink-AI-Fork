namespace Project.BLL.DTOs.Dashboard;

public class ParentDashboardDto
{
    public List<ParentChildDto> Children { get; set; } = new();
    public List<string> RecentActivities { get; set; } = new();
}

public class ParentChildDto
{
    public string Name { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public decimal Performance { get; set; }
    public ChildGradesDto Grades { get; set; } = new();
    public int Absences { get; set; }

    // ── New fields ──────────────────────────────
    public double AttendanceRate { get; set; }
    public int ExcusedAbsences { get; set; }
    public int UnexcusedAbsences { get; set; }
    public string CurrentTermName { get; set; } = string.Empty;

    public List<ChildSubjectDto> SubjectPerformances { get; set; } = new();
    public string? RecommendationsText { get; set; }
    public List<RecommendationSectionDto> RecommendationSections { get; set; } = new();
    public List<ChildUpcomingExamDto> UpcomingExams { get; set; } = new();
    public List<WeeklyPerformanceDto> WeeklyPerformances { get; set; } = new();
    public List<MonthlyExamResultDto> MonthlyExams { get; set; } = new();
    public List<FinalExamResultDto> FinalExams { get; set; } = new();
}

public class ChildGradesDto
{
    public string Last { get; set; } = "—";
    public string Total { get; set; } = "—";
}

public class ChildSubjectDto
{
    public string SubjectName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
}

public class RecommendationSectionDto
{
    public string Title { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}

public class ChildUpcomingExamDto
{
    public string Title { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public decimal TotalScore { get; set; }
}

public class WeeklyPerformanceDto
{
    public string PeriodName { get; set; } = string.Empty;
    public int WeekNumber { get; set; }
    public decimal AvgScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal TotalScore { get; set; }
    public decimal TotalMaxScore { get; set; }
}

public class MonthlyExamResultDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
}

public class FinalExamResultDto
{
    public string SubjectName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
}
