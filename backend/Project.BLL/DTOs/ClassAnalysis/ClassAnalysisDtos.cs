namespace Project.BLL.DTOs.ClassAnalysis;

using Project.BLL.DTOs.Common;

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

public class TeacherGrowthDashboardDto
{
    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public int? Term { get; set; }
    public int EvaluatedWeeks { get; set; }
    public int TotalConfiguredWeeks { get; set; }
    public int TeachersCount { get; set; }
    public double SchoolGrowthRate { get; set; }
    public double SchoolAverageChange { get; set; }
    public double ImprovedStudentsRate { get; set; }
    public double DeclinedStudentsRate { get; set; }
    // Raw student counts at school level
    public int TotalImprovedCount { get; set; }
    public int TotalDeclinedCount { get; set; }
    public int TotalEvaluatedCount { get; set; }
    public List<TeacherGrowthCardDto> Teachers { get; set; } = new();
    public List<TeacherGrowthWeekDto> WeeklyTrend { get; set; } = new();
    public List<TeacherWeeklyTrendDto> TeachersWeeklyTrend { get; set; } = new();
    public List<TeacherGrowthSignalDto> Signals { get; set; } = new();
}

/// <summary>
/// Lightweight overview for the growth dashboard (KPIs + charts + signals, no teacher cards).
/// </summary>
public class TeacherGrowthOverviewDto
{
    public int AcademicYearId { get; set; }
    public string AcademicYearName { get; set; } = string.Empty;
    public int? Term { get; set; }
    public int TeachersCount { get; set; }
    public int EvaluatedWeeks { get; set; }
    public int TotalConfiguredWeeks { get; set; }
    public double SchoolGrowthRate { get; set; }
    public double SchoolAverageChange { get; set; }
    public double ImprovedStudentsRate { get; set; }
    public double DeclinedStudentsRate { get; set; }
    public int TotalImprovedCount { get; set; }
    public int TotalDeclinedCount { get; set; }
    public int TotalEvaluatedCount { get; set; }
    public List<TeacherGrowthWeekDto> WeeklyTrend { get; set; } = new();
    public List<TeacherWeeklyTrendDto> TeachersWeeklyTrend { get; set; } = new();
    public List<TeacherGrowthSignalDto> Signals { get; set; } = new();
}

/// <summary>
/// Teacher cards list only (table + ranking + watch list).
/// </summary>
public class TeacherGrowthTeachersDto
{
    public List<TeacherGrowthCardDto> Teachers { get; set; } = new();
}

public class TeacherGrowthCardDto
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public int StudentsCount { get; set; }
    public int EvaluatedStudentsCount { get; set; }
    public int EvaluatedWeeks { get; set; }
    public int TotalConfiguredWeeks { get; set; }
    public double FirstHalfAverage { get; set; }
    public double SecondHalfAverage { get; set; }
    public double AverageChange { get; set; }
    public double GrowthRate { get; set; }
    public double ImprovedStudentsRate { get; set; }
    public double DeclinedStudentsRate { get; set; }
    public double StableStudentsRate { get; set; }
    public double ExamGrowthRate { get; set; }
    public string Momentum { get; set; } = "stable";
    public string RiskLevel { get; set; } = "watch";
    // Raw counts for proper school-level weighted averaging
    public int ImprovedCount { get; set; }
    public int DeclinedCount { get; set; }
    public int StableCount { get; set; }
}

public class TeacherGrowthWeekDto
{
    public int PeriodId { get; set; }
    public string Label { get; set; } = string.Empty;
    public int OrderNum { get; set; }
    public double AverageScore { get; set; }
    public int EvaluationsCount { get; set; }
}

public class TeacherWeeklyTrendDto
{
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double FirstHalfAverage { get; set; }
    public double SecondHalfAverage { get; set; }
    public List<TeacherGrowthWeekDto> WeeklyScores { get; set; } = new();
}

public class TeacherGrowthSignalDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public int? TeacherId { get; set; }
    public string? TeacherName { get; set; }
}

public class TeacherGrowthStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double FirstHalfAverage { get; set; }
    public double SecondHalfAverage { get; set; }
    public double Change { get; set; }
    public string Status { get; set; } = "stable";
    public int EvaluatedWeeks { get; set; }
}

public class TeacherGrowthStudentPageDto
{
    public TeacherGrowthCardDto Summary { get; set; } = null!;
    public PagedResult<TeacherGrowthStudentDto> Students { get; set; } = new();
}

public class StudentGrowthWeekDetailDto
{
    public string PeriodName { get; set; } = string.Empty;
    public int OrderNum { get; set; }
    public double Score { get; set; }
    public double MaxScore { get; set; }
    public double Percentage { get; set; }
    public bool IsFirstHalf { get; set; }
}

// ── Student Growth Rankings (Top 10 / Bottom 10) ─────────────

public class StudentGrowthRankingItemDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double Change { get; set; }
    public double FirstHalfAverage { get; set; }
    public double SecondHalfAverage { get; set; }
    public string Status { get; set; } = "stable";
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public double AverageMaxScore { get; set; }
    public double MaxPerPeriod { get; set; }
    // Monthly exam breakdown
    public double MonthlyExam1Score { get; set; }
    public double MonthlyExam1Max { get; set; }
    public double MonthlyExam2Score { get; set; }
    public double MonthlyExam2Max { get; set; }
}

public class StudentGrowthRankingDto
{
    public List<StudentGrowthRankingItemDto> TopImproved { get; set; } = new();
    public List<StudentGrowthRankingItemDto> TopDeclined { get; set; } = new();
    public List<StudentGrowthRankingItemDto> TopEvaluationStudents { get; set; } = new();
    public List<StudentGrowthRankingItemDto> TopMonthlyExamStudents { get; set; } = new();
    public List<GradeLevelRankingGroupDto> TopFinalExamStudentsByGrade { get; set; } = new();
}

public class GradeLevelRankingGroupDto
{
    public int GradeLevelId { get; set; }
    public string GradeLevelName { get; set; } = string.Empty;
    public List<StudentGrowthRankingItemDto> Students { get; set; } = new();
}

// ── Student Monthly Exam Summary ──────────────────────────────

public class StudentSubjectExamDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public double? MonthlyExam1Score { get; set; }
    public double? MonthlyExam1Max { get; set; }
    public double? MonthlyExam1Percent { get; set; }
    public double? MonthlyExam2Score { get; set; }
    public double? MonthlyExam2Max { get; set; }
    public double? MonthlyExam2Percent { get; set; }
    public string Status { get; set; } = "stable"; // improved / declined / stable
}

public class StudentExamSummaryDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<StudentSubjectExamDto> Subjects { get; set; } = new();
}

public class StudentFinalGradeSubjectDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public double FinalExamScore { get; set; }
    public double WrittenTotal { get; set; }
    public double Total { get; set; }
    public double MaxTotal { get; set; }
    public double Percentage { get; set; }
}

public class StudentFinalGradeSummaryDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<StudentFinalGradeSubjectDto> Subjects { get; set; } = new();
}

// ── Class × Subject × Teacher Board ─────────────────────────
public class ClassSubjectTeacherBoardItemDto
{
    public int ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string GradeLevelName { get; set; } = string.Empty;
    public List<SubjectTeacherEntryDto> Subjects { get; set; } = new();
}

public class SubjectTeacherEntryDto
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int StudentsCount { get; set; }
}

public class ClassSubjectTeacherBoardDto
{
    public List<ClassSubjectTeacherBoardItemDto> Classes { get; set; } = new();
}
