using Project.Domain.Enums;

namespace Project.BLL.DTOs.Reports;

public class SubjectGradeDto
{
    public string SubjectName { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public double Percentage => MaxScore > 0 ? (double)(Score / MaxScore * 100) : 0;
}

public class MetricDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Max { get; set; } = 100;
    public string Trend { get; set; } = "stable"; // up, down, stable
}

/// <summary>
/// بيانات فترة سابقة للمقارنة (شهريّة)
/// </summary>
public class PeriodComparisonDto
{
    public int PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public double OverallScore { get; set; }
    public double OverallMax { get; set; } = 100;
    public double FinalGradeAverage { get; set; }
    public double FinalGradeMax { get; set; } = 100;
    public List<SubjectGradeDto> SubjectGrades { get; set; } = new();
    public List<MetricDto> Metrics { get; set; } = new();
}

public class StudentReportDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int? PeriodId { get; set; }
    public string? PeriodName { get; set; }
    public AcademicTerm? Term { get; set; }

    // Overall
    public double OverallScore { get; set; }
    public double OverallMax { get; set; } = 100;
    public string OverallTrend { get; set; } = "stable";
    public double OverallChange { get; set; }

    /// <summary>متوسط الدرجات النهائية من FinalGrade (كل مادة من 100)</summary>
    public double FinalGradeAverage { get; set; }
    public double FinalGradeMax { get; set; } = 100;

    // Subject-level grades
    public List<SubjectGradeDto> SubjectGrades { get; set; } = new();

    // General metrics
    public List<MetricDto> Metrics { get; set; } = new();

    // AI-generated texts
    public string? ReportText { get; set; }
    public string? RecommendationsText { get; set; }

    // ── Comparison with previous month ─────────────────────────────
    /// <summary>بيانات الشهر السابق للمقارنة (إن وُجدت)</summary>
    public PeriodComparisonDto? PreviousMonth { get; set; }
}

public class RecommendationSection
{
    public string Title { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}

public class RecommendationsDto
{
    public int StudentId { get; set; }
    public string? RecommendationsText { get; set; }
    public List<string> RecommendationItems { get; set; } = new();
    public List<RecommendationSection> Sections { get; set; } = new();
}
