namespace Project.BLL.DTOs.Reports;

// ───────────────────── Top-level response ─────────────────────

public class AcademicReportDto
{
    public string ClassName { get; set; } = string.Empty;
    public string TermLabel { get; set; } = string.Empty;
    public int StudentCount { get; set; }
    public string SubjectName { get; set; } = string.Empty;

    // KPI summary
    public double AvgPercent { get; set; }
    public double AvgAssessment1 { get; set; }
    public double AvgAssessment2 { get; set; }
    public double AvgFinal { get; set; }

    // Periods & month groups
    public List<PeriodDto> WeeklyPeriods { get; set; } = new();
    public List<MonthGroupDto> MonthGroups { get; set; } = new();

    // Student rows
    public List<StudentReportRowDto> Students { get; set; } = new();

    // Monthly exam details
    public List<MonthlyExamEntryDto> MonthlyExams { get; set; } = new();

    // Quick lists
    public List<TopStudentDto> TopStudents { get; set; } = new();
    public List<StudentSummaryDto> StudentsNeedingSupport { get; set; } = new();
}

// ───────────────────── Period / Month ─────────────────────

public class PeriodDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? MonthName { get; set; }
    public int OrderNum { get; set; }
}

public class MonthGroupDto
{
    public string MonthName { get; set; } = string.Empty;
    public List<PeriodDto> Periods { get; set; } = new();
}

// ───────────────────── Student rows ─────────────────────

public class StudentReportRowDto
{
    public int EnrollmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<WeeklyScoreDto> WeeklyScores { get; set; } = new();
    public double Assessment1 { get; set; }
    public double Assessment2 { get; set; }
    public double TotalMonthly { get; set; }
    public double FinalTotal { get; set; }
    public double MaxTotal { get; set; }
    public double Percentage { get; set; }
}

public class WeeklyScoreDto
{
    public int PeriodId { get; set; }
    public string PeriodName { get; set; } = string.Empty;
    public double Avg { get; set; }
    public double Max { get; set; }
    public double RawScore { get; set; }
    public double RawMax { get; set; }
}

// ───────────────────── Monthly exams ─────────────────────

public class MonthlyExamEntryDto
{
    public int EnrollmentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double Exam1Score { get; set; }
    public double Exam1Max { get; set; }
    public string Exam1Month { get; set; } = string.Empty;
    public double Exam2Score { get; set; }
    public double Exam2Max { get; set; }
    public string Exam2Month { get; set; } = string.Empty;
    public double SemesterScore { get; set; }
    public double SemesterMax { get; set; }
}

// ───────────────────── Summary helpers ─────────────────────

public class TopStudentDto
{
    public string Name { get; set; } = string.Empty;
    public double Percentage { get; set; }
}

public class StudentSummaryDto
{
    public string Name { get; set; } = string.Empty;
    public double Percentage { get; set; }
}
