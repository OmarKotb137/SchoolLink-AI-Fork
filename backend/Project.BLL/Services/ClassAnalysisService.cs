using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.ClassAnalysis;
using Project.BLL.DTOs.Common;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class ClassAnalysisService : IClassAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAcademicYearService _academicYearService;

    public ClassAnalysisService(IUnitOfWork unitOfWork, IAcademicYearService academicYearService)
    {
        _unitOfWork = unitOfWork;
        _academicYearService = academicYearService;
    }

    // ── Pre-fetch cache for teacher growth dashboard ─────────────────────
    private class GrowthPreFetchData
    {
        public List<EvaluationPeriod> Periods { get; set; } = new();
        public List<StudentEnrollment> AllEnrollments { get; set; } = new();
        public List<int> AllEnrollmentIds { get; set; } = new();
        public Dictionary<int, User> Teachers { get; set; } = new();
        public Dictionary<int, Subject> Subjects { get; set; } = new();
        public Dictionary<int, SchoolClass> Classes { get; set; } = new();
        public Dictionary<int, GradeLevel> GradeLevels { get; set; } = new();
        public Dictionary<int, Student> Students { get; set; } = new();

        /// <summary>
        /// Cache key = (classId, periodId, subjectId) → (enrollmentId, percent, score, maxScore).
        /// </summary>
        public Dictionary<(int ClassId, int PeriodId, int SubjectId), List<(int EnrollmentId, double Percent, double Score, double MaxScore)>> EvalCache { get; set; } = new();
    }

    public async Task<OperationResult<ClassAnalysisFullDto>> GetFullAnalysisAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<ClassAnalysisFullDto>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;

        var overview = await GetOverviewInternalAsync(classEntity, resolvedTerm);
        var subjects = await GetSubjectPerformanceInternalAsync(classEntity, resolvedTerm);
        var attendance = await GetAttendanceTrendsInternalAsync(classEntity, resolvedTerm);
        var top = await GetTopStudentsInternalAsync(classEntity, 10, resolvedTerm);
        var atRisk = await GetAtRiskStudentsInternalAsync(classEntity, resolvedTerm);
        var weakness = await GetWeaknessAnalysisInternalAsync(classEntity, resolvedTerm);
        var students = await GetStudentsInternalAsync(classEntity, resolvedTerm);

        var dto = new ClassAnalysisFullDto
        {
            Overview = overview,
            SubjectPerformance = subjects,
            AttendanceTrends = attendance,
            TopStudents = top,
            AtRiskStudents = atRisk,
            WeaknessAnalysis = weakness,
            Students = students
        };

        return OperationResult<ClassAnalysisFullDto>.Success(dto, "تم تحميل تحليل الفصل بنجاح");
    }

    public async Task<OperationResult<ClassAnalysisOverviewDto>> GetOverviewAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<ClassAnalysisOverviewDto>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var dto = await GetOverviewInternalAsync(classEntity, resolvedTerm);
        return OperationResult<ClassAnalysisOverviewDto>.Success(dto);
    }

    public async Task<OperationResult<List<SubjectPerformanceDto>>> GetSubjectPerformanceAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<List<SubjectPerformanceDto>>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var list = await GetSubjectPerformanceInternalAsync(classEntity, resolvedTerm);
        return OperationResult<List<SubjectPerformanceDto>>.Success(list);
    }

    public async Task<OperationResult<List<AttendanceTrendDto>>> GetAttendanceTrendsAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<List<AttendanceTrendDto>>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var list = await GetAttendanceTrendsInternalAsync(classEntity, resolvedTerm);
        return OperationResult<List<AttendanceTrendDto>>.Success(list);
    }

    public async Task<OperationResult<List<TopStudentDto>>> GetTopStudentsAsync(int classId, int count = 10, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<List<TopStudentDto>>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var list = await GetTopStudentsInternalAsync(classEntity, count, resolvedTerm);
        return OperationResult<List<TopStudentDto>>.Success(list);
    }

    public async Task<OperationResult<List<AtRiskStudentDto>>> GetAtRiskStudentsAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<List<AtRiskStudentDto>>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var list = await GetAtRiskStudentsInternalAsync(classEntity, resolvedTerm);
        return OperationResult<List<AtRiskStudentDto>>.Success(list);
    }

    public async Task<OperationResult<List<WeaknessDto>>> GetWeaknessAnalysisAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<List<WeaknessDto>>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var list = await GetWeaknessAnalysisInternalAsync(classEntity, resolvedTerm);
        return OperationResult<List<WeaknessDto>>.Success(list);
    }

    public async Task<OperationResult<List<ClassStudentListDto>>> GetStudentsAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<List<ClassStudentListDto>>.Failure("الفصل غير موجود", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var list = await GetStudentsInternalAsync(classEntity, resolvedTerm);
        return OperationResult<List<ClassStudentListDto>>.Success(list);
    }

    // -----------------------------------------------------------------------
    //  Internal implementations
    // -----------------------------------------------------------------------

    private async Task<ClassAnalysisOverviewDto> GetOverviewInternalAsync(SchoolClass classEntity, AcademicTerm? term)
    {
        // Active enrollments
        var enrollments = await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.ClassId == classEntity.Id && e.LeftAt == null);
        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var totalStudents = enrollmentIds.Count;

        // Class average from FinalGrade
        var classAvg = await _unitOfWork.FinalGrades.GetClassAverageAsync(classEntity.Id, term);

        // Top students count (> 90%)
        var topGrades = await _unitOfWork.FinalGrades.GetTopStudentsByClassAsync(classEntity.Id, 999, term);
        var topCount = topGrades.Count(fg => fg.Total > 0);

        // At-risk students (< 60%)
        var atRiskGrades = await _unitOfWork.FinalGrades.GetStudentsNeedingSupportAsync(classEntity.Id, 60, term);
        var atRiskCount = atRiskGrades.Count();

        // Attendance rate
        var attendanceRate = await CalculateClassAttendanceRateAsync(enrollmentIds, classEntity.AcademicYearId, term);

        // Class name with grade level
        var gradeLevel = classEntity.GradeLevel?.Name ?? "";

        return new ClassAnalysisOverviewDto
        {
            ClassId = classEntity.Id,
            ClassName = classEntity.Name,
            GradeLevelName = gradeLevel,
            TotalStudents = totalStudents,
            ClassAverage = (double)classAvg,
            MaxScore = 100,
            ClassAverageChange = 0, // Would need historical comparison
            TopStudentsCount = topCount > 10 ? 10 : topCount,
            AtRiskStudentsCount = atRiskCount,
            AttendanceRate = attendanceRate
        };
    }

    private async Task<List<SubjectPerformanceDto>> GetSubjectPerformanceInternalAsync(SchoolClass classEntity, AcademicTerm? term)
    {
        var result = new List<SubjectPerformanceDto>();

        // Get all subjects taught in this class
        var assignments = await _unitOfWork.ClassSubjectTeachers
            .FindAsync(cst => cst.ClassId == classEntity.Id && !cst.IsDeleted);

        var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();
        var subjects = (await _unitOfWork.Subjects.FindAsync(s => subjectIds.Contains(s.Id))).ToList();
        var subjectDict = subjects.ToDictionary(s => s.Id, s => s.Name);

        // Get average of OTHER classes at the SAME grade level
        var sameGradeClasses = await _unitOfWork.Classes.FindAsync(c =>
            c.GradeLevelId == classEntity.GradeLevelId
            && c.AcademicYearId == classEntity.AcademicYearId
            && !c.IsDeleted
            && c.Id != classEntity.Id);
        var sameGradeClassIds = sameGradeClasses.Select(c => c.Id).ToList();

        foreach (var subjId in subjectIds)
        {
            var className = subjectDict.GetValueOrDefault(subjId, $"مادة {subjId}");

            // Class average for this subject
            var classGrades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classEntity.Id, term, subjId);
            var classAvg = classGrades.Count > 0
                ? classGrades.Where(fg => fg.MaxTotal > 0).Average(fg => (double)(fg.Total / fg.MaxTotal * 100))
                : 0;

            // Average of same grade level (other classes only)
            double gradeAvg = 0;
            if (sameGradeClassIds.Count > 0)
            {
                var allGradeGrades = new List<FinalGrade>();
                foreach (var cId in sameGradeClassIds)
                {
                    var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(cId, term, subjId);
                    allGradeGrades.AddRange(grades);
                }
                gradeAvg = allGradeGrades.Count > 0
                    ? allGradeGrades.Where(fg => fg.MaxTotal > 0).Average(fg => (double)(fg.Total / fg.MaxTotal * 100))
                    : 0;
            }

            result.Add(new SubjectPerformanceDto
            {
                SubjectId = subjId,
                SubjectName = className,
                ClassAverage = Math.Round(classAvg, 1),
                SchoolAverage = Math.Round(gradeAvg, 1),
                MaxScore = 100
            });
        }

        return result;
    }

    private async Task<List<AttendanceTrendDto>> GetAttendanceTrendsInternalAsync(SchoolClass classEntity, AcademicTerm? term)
    {
        var result = new List<AttendanceTrendDto>();

        var enrollments = await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.ClassId == classEntity.Id && e.LeftAt == null);
        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        if (enrollmentIds.Count == 0) return result;

        // Determine date range based on term or current year
        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null) return result;

        var (startDate, endDate) = GetTermDateRange(currentYear, term);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveEnd = today < endDate ? today : endDate;

        // Group by month
        var current = startDate;
        while (current <= effectiveEnd)
        {
            var monthStart = new DateOnly(current.Year, current.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            if (monthEnd > effectiveEnd) monthEnd = effectiveEnd;

            // Count school days in this month (Sun-Thu)
            var schoolDays = 0;
            var d = monthStart;
            while (d <= monthEnd)
            {
                if (d.DayOfWeek != DayOfWeek.Friday && d.DayOfWeek != DayOfWeek.Saturday)
                    schoolDays++;
                d = d.AddDays(1);
            }

            // Count absences in this month
            var absences = await _unitOfWork.DailyAbsences
                .FindAsync(a => enrollmentIds.Contains(a.EnrollmentId)
                    && a.AbsenceDate >= monthStart && a.AbsenceDate <= monthEnd);

            var distinctAbsenceDays = absences
                .Where(a => a.IsAbsent)
                .Select(a => a.AbsenceDate)
                .Distinct()
                .Count();

            var attendanceRate = schoolDays > 0
                ? Math.Round((double)(schoolDays - distinctAbsenceDays) / schoolDays * 100, 1)
                : 100;

            var monthNames = new[] { "", "يناير", "فبراير", "مارس", "إبريل", "مايو", "يونيو",
                "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر" };

            result.Add(new AttendanceTrendDto
            {
                Month = monthNames[monthStart.Month],
                MonthNumber = monthStart.Month,
                Year = monthStart.Year,
                AttendanceRate = attendanceRate,
                AbsenceRate = Math.Round(100 - attendanceRate, 1),
                TotalSchoolDays = schoolDays,
                AbsenceDays = distinctAbsenceDays
            });

            current = monthStart.AddMonths(1);
        }

        return result;
    }

    private async Task<List<TopStudentDto>> GetTopStudentsInternalAsync(SchoolClass classEntity, int count, AcademicTerm? term)
    {
        var result = new List<TopStudentDto>();

        var enrollments = await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.ClassId == classEntity.Id && e.LeftAt == null);

        var studentAverages = new List<(int StudentId, string Name, double Avg)>();

        foreach (var enrollment in enrollments)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId);
            if (student == null || student.IsDeleted) continue;

            var avg = await CalculateStudentAverageAsync(enrollment.Id, term);
            studentAverages.Add((student.Id, student.FullName, avg));
        }

        int rank = 1;
        foreach (var sa in studentAverages.OrderByDescending(s => s.Avg).Take(count))
        {
            result.Add(new TopStudentDto
            {
                StudentId = sa.StudentId,
                StudentName = sa.Name,
                AverageScore = sa.Avg,
                MaxScore = 100,
                Rank = rank++,
                PhotoUrl = null
            });
        }

        return result;
    }

    private async Task<List<AtRiskStudentDto>> GetAtRiskStudentsInternalAsync(SchoolClass classEntity, AcademicTerm? term)
    {
        var result = new List<AtRiskStudentDto>();

        var atRiskGrades = await _unitOfWork.FinalGrades.GetStudentsNeedingSupportAsync(classEntity.Id, 60, term);

        foreach (var fg in atRiskGrades)
        {
            var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(fg.EnrollmentId);
            var student = enrollment != null ? await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId) : null;

            // Get weak subjects (score < 60%)
            var weakSubjects = new List<string>();
            var subjectGrades = await _unitOfWork.FinalGrades.GetByEnrollmentIdAsync(fg.EnrollmentId, term);
            // (In a real scenario, this would check per-subject; simplified here)

            var score = fg.MaxTotal > 0 ? (double)(fg.Total / fg.MaxTotal * 100) : 0;
            var severity = score < 40 ? "critical" : score < 50 ? "danger" : "warning";

            // Attendance rate for this student
            double attendance = 100;
            if (enrollment != null)
            {
                var absences = await _unitOfWork.DailyAbsences
                    .FindAsync(a => a.EnrollmentId == enrollment.Id && !a.IsDeleted);
                var absCount = absences.Count(a => a.IsAbsent);
                var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
                if (currentYear != null)
                {
                    var (startDate, endDate) = GetTermDateRange(currentYear, term);
                    var today = DateOnly.FromDateTime(DateTime.UtcNow);
                    var effectiveEnd = today < endDate ? today : endDate;
                    var totalDays = 0;
                    for (var d = startDate; d <= effectiveEnd; d = d.AddDays(1))
                    {
                        if (d.DayOfWeek != DayOfWeek.Friday && d.DayOfWeek != DayOfWeek.Saturday)
                            totalDays++;
                    }
                    attendance = totalDays > 0 ? Math.Round((double)(totalDays - absCount) / totalDays * 100, 1) : 100;
                }
            }

            result.Add(new AtRiskStudentDto
            {
                StudentId = student?.Id ?? 0,
                StudentName = student?.FullName ?? $"طالب {fg.EnrollmentId}",
                AverageScore = Math.Round(score, 1),
                MaxScore = 100,
                AttendanceRate = attendance,
                WeakSubjects = weakSubjects,
                Severity = severity
            });
        }

        return result;
    }

    private async Task<List<WeaknessDto>> GetWeaknessAnalysisInternalAsync(SchoolClass classEntity, AcademicTerm? term)
    {
        var result = new List<WeaknessDto>();

        // Get evaluation items to identify skill areas
        var assignments = await _unitOfWork.ClassSubjectTeachers
            .FindAsync(cst => cst.ClassId == classEntity.Id && !cst.IsDeleted);
        var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToList();
        var subjects = (await _unitOfWork.Subjects.FindAsync(s => subjectIds.Contains(s.Id)))
            .ToDictionary(s => s.Id, s => s.Name);

        var enrollments = await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.ClassId == classEntity.Id && e.LeftAt == null);
        var enrollmentIds = enrollments.Select(e => e.Id).ToList();

        // Get evaluation templates for this grade level
        var gradeLevelId = classEntity.GradeLevelId;
        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear != null)
        {
            var templates = await _unitOfWork.EvaluationTemplates
                .GetByGradeLevelAndYearAsync(gradeLevelId, currentYear.Id, term, default);
            var templateIds = templates.Select(t => t.Id).ToList();

            var items = (await _unitOfWork.EvaluationItems
                .FindAsync(i => templateIds.Contains(i.TemplateId) && !i.IsDeleted)).ToList();

            // Group by subject
            foreach (var subjId in subjectIds)
            {
                var subjName = subjects.GetValueOrDefault(subjId, $"مادة {subjId}");
                var templateIdsForSubject = templates
                    .Where(t => t.SubjectId == subjId)
                    .Select(t => t.Id).ToHashSet();

                var subjectItems = items.Where(i => templateIdsForSubject.Contains(i.TemplateId)).ToList();
                if (subjectItems.Count == 0) continue;

                // Calculate average scores for each item if possible
                // (Simplified: calculate from FinalGrade per subject)
                var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classEntity.Id, term, subjId);
                var avgScore = grades.Count > 0
                    ? grades.Where(g => g.MaxTotal > 0).Average(g => (double)(g.Total / g.MaxTotal * 100))
                    : 0;

                var severity = avgScore >= 80 ? "safe"
                    : avgScore >= 65 ? "low"
                    : avgScore >= 50 ? "medium"
                    : "critical";

                // Create a weakness entry per subject based on average
                result.Add(new WeaknessDto
                {
                    SkillName = $"{subjName}",
                    SubjectId = subjId,
                    SubjectName = subjName,
                    Severity = severity,
                    AverageScore = Math.Round(avgScore, 1),
                    MaxScore = 100
                });
            }
        }

        return result;
    }

    private async Task<List<ClassStudentListDto>> GetStudentsInternalAsync(SchoolClass classEntity, AcademicTerm? term)
    {
        var result = new List<ClassStudentListDto>();

        var enrollments = await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.ClassId == classEntity.Id && e.LeftAt == null);

        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        var (startDate, endDate) = GetTermDateRange(currentYear, term);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveEnd = today < endDate ? today : endDate;

        foreach (var enrollment in enrollments)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId);
            if (student == null || student.IsDeleted) continue;

            // Student average (across ALL subjects)
            var avgScore = await CalculateStudentAverageAsync(enrollment.Id, term);

            // Absence count
            var absences = await _unitOfWork.DailyAbsences
                .FindAsync(a => a.EnrollmentId == enrollment.Id && a.AbsenceDate >= startDate
                    && a.AbsenceDate <= effectiveEnd);
            var absCount = absences.Count(a => a.IsAbsent);

            // Attendance rate
            var totalDays = 0;
            for (var d = startDate; d <= effectiveEnd; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Friday && d.DayOfWeek != DayOfWeek.Saturday)
                    totalDays++;
            }
            var attendanceRate = totalDays > 0 ? Math.Round((double)(totalDays - absCount) / totalDays * 100, 1) : 100;

            var status = avgScore >= 90 ? "excellent" : avgScore < 60 ? "at-risk" : "active";

            result.Add(new ClassStudentListDto
            {
                StudentId = student.Id,
                StudentName = student.FullName,
                AverageScore = Math.Round(avgScore, 1),
                AttendanceRate = attendanceRate,
                AbsenceCount = absCount,
                Status = status
            });
        }

        return result;
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Calculates a student's average score across ALL subjects using FinalGrade records.
    /// Sums all subject totals and maxTotals, then computes percentage.
    /// </summary>
    private async Task<double> CalculateStudentAverageAsync(int enrollmentId, AcademicTerm? term)
    {
        double avg = 0;

        // Get ALL final grades for this enrollment (per-subject)
        var allGrades = await _unitOfWork.FinalGrades.FindAsync(fg =>
            fg.EnrollmentId == enrollmentId && !fg.IsDeleted);
        var grades = term.HasValue
            ? allGrades.Where(g => g.Term == term.Value).ToList()
            : allGrades.ToList();

        if (grades.Count > 0)
        {
            // Try per-subject grades first (SubjectId != null)
            var subjectGrades = grades.Where(g => g.SubjectId != null).ToList();
            if (subjectGrades.Count > 0)
            {
                var totalSum = subjectGrades.Sum(g => (double)g.Total);
                var maxSum = subjectGrades.Sum(g => (double)g.MaxTotal);
                avg = maxSum > 0 ? totalSum / maxSum * 100 : 0;
            }
            else
            {
                // Fallback to overall grade (SubjectId == null)
                var overall = grades.FirstOrDefault();
                if (overall?.MaxTotal > 0)
                    avg = (double)(overall.Total / overall.MaxTotal * 100);
            }
        }

        return Math.Round(avg, 1);
    }

    private async Task<double> CalculateClassAttendanceRateAsync(List<int> enrollmentIds, int academicYearId, AcademicTerm? term)
    {
        if (enrollmentIds.Count == 0) return 100;

        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null) return 100;

        var (startDate, endDate) = GetTermDateRange(currentYear, term);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveEnd = today < endDate ? today : endDate;

        // Total school days
        var totalSchoolDays = 0;
        for (var d = startDate; d <= effectiveEnd; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Friday && d.DayOfWeek != DayOfWeek.Saturday)
                totalSchoolDays++;
        }

        if (totalSchoolDays == 0) return 100;

        // Total absences across all enrollments
        var allAbsences = await _unitOfWork.DailyAbsences
            .FindAsync(a => enrollmentIds.Contains(a.EnrollmentId)
                && a.AbsenceDate >= startDate && a.AbsenceDate <= effectiveEnd);

        var totalAbsenceDays = allAbsences.Count(a => a.IsAbsent);

        // Average across all students
        var possibleAttendanceDays = totalSchoolDays * enrollmentIds.Count;
        var actualAttendanceDays = possibleAttendanceDays - totalAbsenceDays;

        return Math.Round((double)actualAttendanceDays / possibleAttendanceDays * 100, 1);
    }

    private (DateOnly Start, DateOnly End) GetTermDateRange(AcademicYear? year, AcademicTerm? term)
    {
        if (year == null)
        {
            var now = DateOnly.FromDateTime(DateTime.UtcNow);
            return (new DateOnly(now.Year, 9, 1), new DateOnly(now.Year + 1, 6, 30));
        }

        return term switch
        {
            AcademicTerm.FirstSemester => (
                year.FirstSemesterStartDate ?? year.StartDate,
                year.FirstSemesterEndDate ?? year.EndDate
            ),
            AcademicTerm.SecondSemester => (
                year.SecondSemesterStartDate ?? year.StartDate,
                year.SecondSemesterEndDate ?? year.EndDate
            ),
            _ => (year.StartDate, year.EndDate)
        };
    }

    // ═══════════════════════════════════════════════════════════
    //  Pre-fetch helper (builds GrowthPreFetchData)
    // ═══════════════════════════════════════════════════════════

    private async Task<GrowthPreFetchData> BuildGrowthPreFetchAsync(
        List<ClassSubjectTeacher> assignments,
        AcademicYear? currentYear,
        AcademicTerm? resolvedTerm)
    {
        var data = new GrowthPreFetchData();
        var allSubjectIds = assignments.Select(a => a.SubjectId).Distinct().ToHashSet();

        var (startDate, endDate) = GetTermDateRange(currentYear, resolvedTerm);

        // 1. Evaluation periods
        data.Periods = (await _unitOfWork.EvaluationPeriods
            .FindAsync(p => p.StartDate >= startDate && p.EndDate <= endDate && !p.IsDeleted))
            .OrderBy(p => p.OrderNum)
            .ToList();

        // 2. All enrollments
        var classIds = assignments.Select(a => a.ClassId).Distinct().ToList();
        data.AllEnrollments = (await _unitOfWork.StudentEnrollments
            .FindAsync(e => classIds.Contains(e.ClassId) && e.AcademicYearId == currentYear.Id && e.LeftAt == null && !e.IsDeleted))
            .ToList();
        var allEnrollmentIds = data.AllEnrollments.Select(e => e.Id).ToList();
        data.AllEnrollmentIds = allEnrollmentIds;

        // enrollment -> classId
        var enrollmentClassMap = data.AllEnrollments
            .GroupBy(e => e.Id)
            .ToDictionary(g => g.Key, g => g.First().ClassId);

        // 3. Teachers, Subjects, Classes, GradeLevels, Students
        var teacherIds = assignments.Select(a => a.TeacherId).Distinct().ToList();
        data.Teachers = (await _unitOfWork.Users.FindAsync(u => teacherIds.Contains(u.Id)))
            .ToDictionary(u => u.Id, u => u);

        data.Subjects = (await _unitOfWork.Subjects.FindAsync(s => allSubjectIds.Contains(s.Id) && !s.IsDeleted))
            .ToDictionary(s => s.Id, s => s);

        data.Classes = (await _unitOfWork.Classes.FindAsync(c => classIds.Contains(c.Id) && !c.IsDeleted))
            .ToDictionary(c => c.Id, c => c);

        var gradeLevelIds = data.Classes.Values.Select(c => c.GradeLevelId).Distinct().ToList();
        data.GradeLevels = (await _unitOfWork.GradeLevels.FindAsync(g => gradeLevelIds.Contains(g.Id)))
            .ToDictionary(g => g.Id, g => g);

        var studentIds = data.AllEnrollments.Select(e => e.StudentId).Distinct().ToList();
        data.Students = (await _unitOfWork.Students.FindAsync(s => studentIds.Contains(s.Id) && !s.IsDeleted))
            .ToDictionary(s => s.Id, s => s);

        // 4. Evaluation cache: single batch DB query for all periods
        var evalCache = new Dictionary<(int, int, int), List<(int, double, double, double)>>();
        var periodIds = data.Periods.Select(p => p.Id).ToList();
        var allEvaluations = await _unitOfWork.StudentEvaluations
            .GetByPeriodsAndEnrollmentsAsync(periodIds, allEnrollmentIds);

        var evalByPeriod = allEvaluations
            .Where(e => e.Score.HasValue && allSubjectIds.Contains(e.EvaluationItem.Template.SubjectId))
            .GroupBy(e => new { e.PeriodId, e.EnrollmentId, SubjectId = e.EvaluationItem.Template.SubjectId })
            .Select(g =>
            {
                var max = g.Sum(e => (double)e.EvaluationItem.MaxScore);
                var score = g.Sum(e => (double)(e.Score ?? 0));
                return new
                {
                    EnrollmentId = g.Key.EnrollmentId,
                    SubjectId = g.Key.SubjectId,
                    PeriodId = g.Key.PeriodId,
                    ClassId = enrollmentClassMap.GetValueOrDefault(g.Key.EnrollmentId, 0),
                    Score = score,
                    MaxScore = max,
                    Percent = max > 0 ? score / max * 100 : 0
                };
            })
            .Where(x => x.ClassId > 0)
            .ToList();

        foreach (var v in evalByPeriod)
        {
            var key = (v.ClassId, v.PeriodId, v.SubjectId);
            if (!evalCache.ContainsKey(key))
                evalCache[key] = new List<(int, double, double, double)>();
            evalCache[key].Add((v.EnrollmentId, v.Percent, v.Score, v.MaxScore));
        }
        data.EvalCache = evalCache;

        return data;
    }

    // ═══════════════════════════════════════════════════════════
    //  Student Growth Rankings (Top 10 / Bottom 10)
    // ═══════════════════════════════════════════════════════════

    public async Task<OperationResult<StudentGrowthRankingDto>> GetStudentGrowthRankingsAsync(
        AcademicTerm? term = null)
    {
        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null)
            return OperationResult<StudentGrowthRankingDto>.Success(new StudentGrowthRankingDto());

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;

        var assignments = (await _unitOfWork.ClassSubjectTeachers.FindAsync(cst =>
            cst.AcademicYearId == currentYear.Id && !cst.IsDeleted)).ToList();

        if (assignments.Count == 0)
            return OperationResult<StudentGrowthRankingDto>.Success(new StudentGrowthRankingDto());

        var cache = await BuildGrowthPreFetchAsync(assignments, currentYear, resolvedTerm);
        var periods = cache.Periods;
        var allEnrollmentIds = cache.AllEnrollmentIds;
        var allSubjectIdsSet = assignments.Select(a => a.SubjectId).Distinct().ToHashSet();

        // Compute max possible total score per period (sum across all subjects)
        var periodSubjectMaxes = new Dictionary<int, Dictionary<int, double>>();
        foreach (var kvp in cache.EvalCache)
        {
            int periodId = kvp.Key.PeriodId;
            int subjectId = kvp.Key.SubjectId;
            double maxScore = kvp.Value.Max(v => v.MaxScore);
            if (!periodSubjectMaxes.ContainsKey(periodId))
                periodSubjectMaxes[periodId] = new Dictionary<int, double>();
            periodSubjectMaxes[periodId][subjectId] = maxScore;
        }
        var maxPerPeriod = periodSubjectMaxes.Count > 0
            ? Math.Round(periodSubjectMaxes.Values.Max(d => d.Values.Sum()), 1)
            : 40.0;

        if (periods.Count == 0)
            return OperationResult<StudentGrowthRankingDto>.Success(new StudentGrowthRankingDto());

        // Build scores per enrollment, keeping SubjectId — single pass over EvalCache
        var periodOrderMap = periods.ToDictionary(p => p.Id, p => p.OrderNum);
        var scoresByEnrollment = cache.AllEnrollments
            .ToDictionary(e => e.Id, _ => new List<(int OrderNum, int SubjectId, double Percent, double Score, double MaxScore)>());

        foreach (var (key, values) in cache.EvalCache)
        {
            var orderNum = periodOrderMap.GetValueOrDefault(key.PeriodId, 0);
            foreach (var (enrollmentId, percent, score, maxScore) in values)
            {
                if (scoresByEnrollment.TryGetValue(enrollmentId, out var list))
                    list.Add((orderNum, key.SubjectId, percent, score, maxScore));
            }
        }

        var evaluatedOrders = scoresByEnrollment.Values
            .SelectMany(s => s.Select(x => x.OrderNum))
            .Distinct()
            .OrderBy(o => o)
            .ToList();
        if (evaluatedOrders.Count == 0)
            return OperationResult<StudentGrowthRankingDto>.Success(new StudentGrowthRankingDto());

        var splitIndex = Math.Max(1, evaluatedOrders.Count / 2);
        var firstOrders = evaluatedOrders.Take(splitIndex).ToHashSet();
        var secondOrders = evaluatedOrders.Skip(splitIndex).ToHashSet();
        if (secondOrders.Count == 0)
            secondOrders = evaluatedOrders.Skip(Math.Max(evaluatedOrders.Count - 1, 0)).ToHashSet();

        // Pre-build student lookup (reused 5x)
        var studentInfo = cache.AllEnrollments
            .GroupBy(e => e.StudentId)
            .Select(g => g.First())
            .ToDictionary(e => e.StudentId, e =>
            {
                var student = cache.Students.GetValueOrDefault(e.StudentId);
                var cls = cache.Classes.GetValueOrDefault(e.ClassId);
                return (Name: student?.FullName ?? $"طالب {e.StudentId}", ClassName: cls?.Name ?? "");
            });

        // Single pass: build studentChanges (per-subject) + studentRawAverages (overall)
        var studentChanges = new List<(int StudentId, double Change, double FirstAvg, double SecondAvg, int SubjectId)>();
        var studentRawAverages = new List<(int StudentId, double AvgPct, double TotalScore, double TotalMax)>();

        foreach (var enrollment in cache.AllEnrollments)
        {
            var scores = scoresByEnrollment.GetValueOrDefault(enrollment.Id);
            if (scores == null || scores.Count == 0) continue;

            // studentRawAverages: overall across all subjects (keep as before)
            studentRawAverages.Add((
                enrollment.StudentId,
                scores.Average(s => s.Percent),
                scores.Sum(s => s.Score),
                scores.Sum(s => s.MaxScore)
            ));

            // studentChanges: per-subject, so subject-level changes aren't diluted
            foreach (var subjectGroup in scores.GroupBy(s => s.SubjectId))
            {
                var subjectScores = subjectGroup.ToList();
                var firstScores = subjectScores.Where(s => firstOrders.Contains(s.OrderNum)).Select(s => s.Percent).ToList();
                var secondScores = subjectScores.Where(s => secondOrders.Contains(s.OrderNum)).Select(s => s.Percent).ToList();
                var firstAvg = firstScores.Count > 0 ? firstScores.Average() : 0;
                var secondAvg = secondScores.Count > 0 ? secondScores.Average() : firstAvg;
                studentChanges.Add((enrollment.StudentId, secondAvg - firstAvg, firstAvg, secondAvg, subjectGroup.Key));
            }
        }

        // Deduplicate by studentId — keep largest |change|
        var deduped = studentChanges
            .GroupBy(x => x.StudentId)
            .Select(g => g.OrderByDescending(x => Math.Abs(x.Change)).First())
            .ToList();

        // Local helper
        StudentGrowthRankingItemDto MakeItem(
            int sid, double change, double firstAvg, double secondAvg,
            string status, int subjectId = 0, double score = 0, double maxScore = 0, double maxP = 0)
        {
            var info = studentInfo.GetValueOrDefault(sid);
            var subj = subjectId > 0 ? cache.Subjects.GetValueOrDefault(subjectId) : null;
            return new StudentGrowthRankingItemDto
            {
                StudentId = sid,
                StudentName = info.Name,
                Change = Math.Round(change, 1),
                FirstHalfAverage = Math.Round(firstAvg, 1),
                SecondHalfAverage = Math.Round(secondAvg, 1),
                Status = status,
                SubjectName = subj?.Name ?? "جميع المواد",
                TeacherName = "",
                ClassName = info.ClassName,
                AverageScore = Math.Round(score, 1),
                AverageMaxScore = Math.Round(maxScore, 1),
                MaxPerPeriod = maxP
            };
        }

        var topImproved = deduped
            .Where(x => x.Change > 0)
            .OrderByDescending(x => x.Change)
            .Take(10)
            .Select(x => MakeItem(x.StudentId, x.Change, x.FirstAvg, x.SecondAvg, "improved", x.SubjectId))
            .ToList();

        var topDeclined = deduped
            .Where(x => x.Change < 0)
            .OrderBy(x => x.Change)
            .Take(10)
            .Select(x => MakeItem(x.StudentId, x.Change, x.FirstAvg, x.SecondAvg, "declined", x.SubjectId))
            .ToList();

        var topEvals = studentRawAverages
            .GroupBy(x => x.StudentId)
            .Select(g => g.OrderByDescending(x => x.AvgPct).First())
            .OrderByDescending(x => x.AvgPct)
            .Take(10)
            .Select(x =>
            {
                var info = studentInfo.GetValueOrDefault(x.StudentId);
                return new StudentGrowthRankingItemDto
                {
                    StudentId = x.StudentId,
                    StudentName = info.Name,
                    Change = 0,
                    FirstHalfAverage = Math.Round(x.AvgPct, 1),
                    Status = "improved",
                    SubjectName = "جميع المواد",
                    TeacherName = "",
                    ClassName = info.ClassName,
                    AverageScore = Math.Round(x.TotalScore, 1),
                    AverageMaxScore = Math.Round(x.TotalMax, 1),
                    MaxPerPeriod = maxPerPeriod
                };
            })
            .ToList();

        // -- Top monthly exam students (each month separately) --
        var allExams = (await _unitOfWork.PeriodicAssessments.FindAsync(pa =>
            allEnrollmentIds.Contains(pa.EnrollmentId) &&
            pa.Term == resolvedTerm &&
            (pa.AssessmentType == PeriodicAssessmentType.MonthlyExam1 ||
             pa.AssessmentType == PeriodicAssessmentType.MonthlyExam2))).ToList();

        var examAverages = allExams
            .GroupBy(pa => pa.EnrollmentId)
            .Select(g =>
            {
                var sid = cache.AllEnrollments.FirstOrDefault(e => e.Id == g.Key)?.StudentId ?? 0;
                var month1 = g.Where(pa => pa.AssessmentType == PeriodicAssessmentType.MonthlyExam1);
                var month2 = g.Where(pa => pa.AssessmentType == PeriodicAssessmentType.MonthlyExam2);
                var m1Score = month1.Sum(pa => (double)pa.Score);
                var m1Max = month1.Sum(pa => (double)pa.MaxScore);
                var m2Score = month2.Sum(pa => (double)pa.Score);
                var m2Max = month2.Sum(pa => (double)pa.MaxScore);
                var combinedPct = (m1Max + m2Max) > 0
                    ? (m1Score + m2Score) / (m1Max + m2Max) * 100
                    : 0;
                return new
                {
                    StudentId = sid,
                    AvgPct = combinedPct,
                    TotalScore = m1Score,
                    TotalMax = m1Max,
                    Month1Score = m1Score,
                    Month1Max = m1Max,
                    Month2Score = m2Score,
                    Month2Max = m2Max
                };
            })
            .Where(x => x.StudentId > 0)
            .GroupBy(x => x.StudentId)
            .Select(g => g.OrderByDescending(x => x.AvgPct).First())
            .OrderByDescending(x => x.AvgPct)
            .Take(10)
            .Select(x =>
            {
                var info = studentInfo.GetValueOrDefault(x.StudentId);
                return new StudentGrowthRankingItemDto
                {
                    StudentId = x.StudentId,
                    StudentName = info.Name,
                    Change = 0,
                    FirstHalfAverage = Math.Round(x.AvgPct, 1),
                    Status = "improved",
                    SubjectName = "جميع المواد",
                    TeacherName = "",
                    ClassName = info.ClassName,
                    AverageScore = Math.Round(x.TotalScore, 1),
                    AverageMaxScore = Math.Round(x.TotalMax, 1),
                    MaxPerPeriod = maxPerPeriod,
                    MonthlyExam1Score = Math.Round(x.Month1Score, 1),
                    MonthlyExam1Max = Math.Round(x.Month1Max, 1),
                    MonthlyExam2Score = Math.Round(x.Month2Score, 1),
                    MonthlyExam2Max = Math.Round(x.Month2Max, 1)
                };
            })
            .ToList();

        // -- Top final exam students --
        var allFinalGrades = (await _unitOfWork.FinalGrades.FindAsync(fg =>
            allEnrollmentIds.Contains(fg.EnrollmentId) &&
            fg.Term == resolvedTerm &&
            !fg.IsDeleted)).ToList();

        var finalExamAverages = allFinalGrades
            .Where(fg => fg.MaxTotal > 0)
            .GroupBy(fg => fg.EnrollmentId)
            .Select(g =>
            {
                var sid = cache.AllEnrollments.FirstOrDefault(e => e.Id == g.Key)?.StudentId ?? 0;
                var avgPct = g.Average(fg => (double)(fg.Total / fg.MaxTotal * 100));
                return new { StudentId = sid, AvgPct = avgPct, TotalScore = g.Sum(fg => (double)fg.Total), TotalMax = g.Sum(fg => (double)fg.MaxTotal) };
            })
            .Where(x => x.StudentId > 0)
            .GroupBy(x => x.StudentId)
            .Select(g => g.OrderByDescending(x => x.AvgPct).First())
            .OrderByDescending(x => x.AvgPct)
            .Take(10)
            .Select(x => MakeItem(x.StudentId, 0, x.AvgPct, 0, "improved", subjectId: 0, score: x.TotalScore, maxScore: x.TotalMax))
            .ToList();

        return OperationResult<StudentGrowthRankingDto>.Success(new StudentGrowthRankingDto
        {
            TopImproved = topImproved,
            TopDeclined = topDeclined,
            TopEvaluationStudents = topEvals,
            TopMonthlyExamStudents = examAverages,
            TopFinalExamStudents = finalExamAverages
        });
    }

    // ═══════════════════════════════════════════════════════════
    //  Student Monthly Exam Summary
    // ═══════════════════════════════════════════════════════════

    public async Task<OperationResult<StudentExamSummaryDto>> GetStudentExamSummaryAsync(
        int studentId,
        AcademicTerm? term = null)
    {
        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null)
            return OperationResult<StudentExamSummaryDto>.Failure("لا توجد سنة دراسية حالية", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;

        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null)
            return OperationResult<StudentExamSummaryDto>.Failure("الطالب غير موجود", 404);

        var enrollments = (await _unitOfWork.StudentEnrollments.FindAsync(e =>
            e.StudentId == studentId &&
            e.AcademicYearId == currentYear.Id &&
            e.LeftAt == null && !e.IsDeleted)).ToList();

        if (enrollments.Count == 0)
            return OperationResult<StudentExamSummaryDto>.Failure("لا يوجد تسجيل لهذا الطالب", 404);

        var result = new StudentExamSummaryDto
        {
            StudentId = studentId,
            StudentName = student.FullName,
            Subjects = new List<StudentSubjectExamDto>()
        };

        foreach (var enrollment in enrollments)
        {
            var assessments = (await _unitOfWork.PeriodicAssessments.FindAsync(pa =>
                pa.EnrollmentId == enrollment.Id &&
                pa.Term == resolvedTerm &&
                (pa.AssessmentType == PeriodicAssessmentType.MonthlyExam1 ||
                 pa.AssessmentType == PeriodicAssessmentType.MonthlyExam2))).ToList();

            if (assessments.Count == 0) continue;

            // Group by SubjectId to show each subject separately
            foreach (var subjectGroup in assessments.Where(a => a.SubjectId.HasValue).GroupBy(a => a.SubjectId!.Value))
            {
                var subjectId = subjectGroup.Key;
                var subject = subjectId > 0
                    ? await _unitOfWork.Subjects.GetByIdAsync(subjectId)
                    : null;

                var exam1 = subjectGroup.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam1);
                var exam2 = subjectGroup.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam2);

                var exam1Percent = exam1 != null && exam1.MaxScore > 0
                    ? (double)(exam1.Score / exam1.MaxScore * 100)
                    : (double?)null;
                var exam2Percent = exam2 != null && exam2.MaxScore > 0
                    ? (double)(exam2.Score / exam2.MaxScore * 100)
                    : (double?)null;

                string status = "stable";
                if (exam1Percent.HasValue && exam2Percent.HasValue)
                {
                    if (exam2Percent.Value > exam1Percent.Value) status = "improved";
                    else if (exam2Percent.Value < exam1Percent.Value) status = "declined";
                }

                result.Subjects.Add(new StudentSubjectExamDto
                {
                    SubjectId = subjectId,
                    SubjectName = subject?.Name ?? $"مادة {subjectId}",
                    MonthlyExam1Score = exam1 != null ? (double)exam1.Score : null,
                    MonthlyExam1Max = exam1 != null ? (double)exam1.MaxScore : null,
                    MonthlyExam1Percent = exam1Percent.HasValue ? Math.Round(exam1Percent.Value, 1) : null,
                    MonthlyExam2Score = exam2 != null ? (double)exam2.Score : null,
                    MonthlyExam2Max = exam2 != null ? (double)exam2.MaxScore : null,
                    MonthlyExam2Percent = exam2Percent.HasValue ? Math.Round(exam2Percent.Value, 1) : null,
                    Status = status
                });
            }
        }

        return OperationResult<StudentExamSummaryDto>.Success(result);
    }

    // ═══════════════════════════════════════════════════════════
    //  Student Final Grades By Subject
    // ═══════════════════════════════════════════════════════════

    public async Task<OperationResult<StudentFinalGradeSummaryDto>> GetStudentFinalGradesAsync(
        int studentId,
        AcademicTerm? term = null)
    {
        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null)
            return OperationResult<StudentFinalGradeSummaryDto>.Failure("لا توجد سنة دراسية حالية", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;

        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null)
            return OperationResult<StudentFinalGradeSummaryDto>.Failure("الطالب غير موجود", 404);

        var enrollments = (await _unitOfWork.StudentEnrollments.FindAsync(e =>
            e.StudentId == studentId &&
            e.AcademicYearId == currentYear.Id &&
            e.LeftAt == null && !e.IsDeleted)).ToList();

        if (enrollments.Count == 0)
            return OperationResult<StudentFinalGradeSummaryDto>.Failure("لا يوجد تسجيل لهذا الطالب", 404);

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();

        var allGrades = (await _unitOfWork.FinalGrades.FindAsync(fg =>
            enrollmentIds.Contains(fg.EnrollmentId) &&
            fg.Term == resolvedTerm &&
            !fg.IsDeleted)).ToList();

        var result = new StudentFinalGradeSummaryDto
        {
            StudentId = studentId,
            StudentName = student.FullName,
            Subjects = new List<StudentFinalGradeSubjectDto>()
        };

        foreach (var grade in allGrades.Where(g => g.SubjectId.HasValue).GroupBy(g => g.SubjectId!.Value))
        {
            var subject = await _unitOfWork.Subjects.GetByIdAsync(grade.Key);
            var total = (double)grade.Sum(g => g.Total);
            var maxTotal = (double)grade.Sum(g => g.MaxTotal);

            result.Subjects.Add(new StudentFinalGradeSubjectDto
            {
                SubjectId = grade.Key,
                SubjectName = subject?.Name ?? $"مادة {grade.Key}",
                FinalExamScore = Math.Round((double)grade.Sum(g => g.FinalExamScore), 1),
                WrittenTotal = Math.Round((double)grade.Sum(g => g.WrittenTotal), 1),
                Total = Math.Round(total, 1),
                MaxTotal = Math.Round(maxTotal, 1),
                Percentage = maxTotal > 0 ? Math.Round(total / maxTotal * 100, 1) : 0
            });
        }

        return OperationResult<StudentFinalGradeSummaryDto>.Success(result);
    }

    // ═══════════════════════════════════════════════════════════
    //  Teacher Growth Dashboard (main + split endpoints)
    // ═══════════════════════════════════════════════════════════

    public async Task<OperationResult<TeacherGrowthDashboardDto>> GetTeacherGrowthDashboardAsync(
        AcademicTerm? term = null,
        int? teacherId = null,
        int? classId = null)
    {
        var (currentYear, resolvedTerm, assignments, cache) = await LoadDashboardCommonAsync(term, teacherId, classId);
        if (currentYear == null)
            return OperationResult<TeacherGrowthDashboardDto>.Failure("No current academic year found", 404);

        var (orderedCards, evaluatedWeeks, configuredWeeks) = await BuildDashboardCardsAsync(assignments, resolvedTerm, cache);

        var weeklyTrend = await BuildTeacherGrowthTrendAsync(assignments, currentYear, resolvedTerm, cache);
        var teachersWeeklyTrend = BuildTeachersWeeklyTrendFromCache(assignments, cache);
        var uniqueCounts = ComputeUniqueStudentGrowthCountsFromCache(assignments, cache);
        var uniqueEvaluated = uniqueCounts.Improved + uniqueCounts.Declined + uniqueCounts.Stable;
        var totalImproved = uniqueCounts.Improved;
        var totalDeclined = uniqueCounts.Declined;
        var schoolImprovedRate = uniqueEvaluated > 0 ? (double)totalImproved / uniqueEvaluated * 100 : 0;
        var schoolDeclinedRate = uniqueEvaluated > 0 ? (double)totalDeclined / uniqueEvaluated * 100 : 0;
        var schoolAvgChange = ComputeAverageChangeFromCache(assignments, cache);

        return OperationResult<TeacherGrowthDashboardDto>.Success(new TeacherGrowthDashboardDto
        {
            AcademicYearId = currentYear.Id,
            AcademicYearName = currentYear.Name,
            Term = resolvedTerm.HasValue ? (int)resolvedTerm.Value : null,
            TeachersCount = orderedCards.Select(c => c.TeacherId).Distinct().Count(),
            EvaluatedWeeks = evaluatedWeeks,
            TotalConfiguredWeeks = configuredWeeks,
            SchoolGrowthRate = Math.Round(schoolImprovedRate, 1),
            SchoolAverageChange = Math.Round(schoolAvgChange, 1),
            ImprovedStudentsRate = Math.Round(schoolImprovedRate, 1),
            DeclinedStudentsRate = Math.Round(schoolDeclinedRate, 1),
            TotalImprovedCount = totalImproved,
            TotalDeclinedCount = totalDeclined,
            TotalEvaluatedCount = uniqueEvaluated,
            Teachers = orderedCards,
            WeeklyTrend = weeklyTrend,
            TeachersWeeklyTrend = teachersWeeklyTrend,
            Signals = BuildTeacherGrowthSignals(orderedCards, evaluatedWeeks, configuredWeeks)
        });
    }

    public async Task<OperationResult<TeacherGrowthOverviewDto>> GetTeacherGrowthDashboardOverviewAsync(
        AcademicTerm? term = null,
        int? teacherId = null,
        int? classId = null)
    {
        var (currentYear, resolvedTerm, assignments, cache) = await LoadDashboardCommonAsync(term, teacherId, classId);
        if (currentYear == null)
            return OperationResult<TeacherGrowthOverviewDto>.Failure("No current academic year found", 404);

        var (orderedCards, evaluatedWeeks, configuredWeeks) = await BuildDashboardCardsAsync(assignments, resolvedTerm, cache);

        var weeklyTrend = await BuildTeacherGrowthTrendAsync(assignments, currentYear, resolvedTerm, cache);
        var teachersWeeklyTrend = BuildTeachersWeeklyTrendFromCache(assignments, cache);
        var uniqueCounts = ComputeUniqueStudentGrowthCountsFromCache(assignments, cache);
        var uniqueEvaluated = uniqueCounts.Improved + uniqueCounts.Declined + uniqueCounts.Stable;
        var totalImproved = uniqueCounts.Improved;
        var totalDeclined = uniqueCounts.Declined;
        var schoolImprovedRate = uniqueEvaluated > 0 ? (double)totalImproved / uniqueEvaluated * 100 : 0;
        var schoolDeclinedRate = uniqueEvaluated > 0 ? (double)totalDeclined / uniqueEvaluated * 100 : 0;
        var schoolAvgChange = ComputeAverageChangeFromCache(assignments, cache);

        return OperationResult<TeacherGrowthOverviewDto>.Success(new TeacherGrowthOverviewDto
        {
            AcademicYearId = currentYear.Id,
            AcademicYearName = currentYear.Name,
            Term = resolvedTerm.HasValue ? (int)resolvedTerm.Value : null,
            TeachersCount = orderedCards.Select(c => c.TeacherId).Distinct().Count(),
            EvaluatedWeeks = evaluatedWeeks,
            TotalConfiguredWeeks = configuredWeeks,
            SchoolGrowthRate = Math.Round(schoolImprovedRate, 1),
            SchoolAverageChange = Math.Round(schoolAvgChange, 1),
            ImprovedStudentsRate = Math.Round(schoolImprovedRate, 1),
            DeclinedStudentsRate = Math.Round(schoolDeclinedRate, 1),
            TotalImprovedCount = totalImproved,
            TotalDeclinedCount = totalDeclined,
            TotalEvaluatedCount = uniqueEvaluated,
            WeeklyTrend = weeklyTrend,
            TeachersWeeklyTrend = teachersWeeklyTrend,
            Signals = BuildTeacherGrowthSignals(orderedCards, evaluatedWeeks, configuredWeeks)
        });
    }

    public async Task<OperationResult<TeacherGrowthTeachersDto>> GetTeacherGrowthDashboardTeachersAsync(
        AcademicTerm? term = null,
        int? teacherId = null,
        int? classId = null)
    {
        var (currentYear, resolvedTerm, assignments, cache) = await LoadDashboardCommonAsync(term, teacherId, classId);
        if (currentYear == null)
            return OperationResult<TeacherGrowthTeachersDto>.Failure("No current academic year found", 404);

        var (orderedCards, _, _) = await BuildDashboardCardsAsync(assignments, resolvedTerm, cache);

        return OperationResult<TeacherGrowthTeachersDto>.Success(new TeacherGrowthTeachersDto
        {
            Teachers = orderedCards
        });
    }

    public async Task<OperationResult<TeacherGrowthStudentPageDto>> GetTeacherGrowthStudentsAsync(
        int teacherId,
        int? classId = null,
        int? subjectId = null,
        AcademicTerm? term = null,
        int page = 1,
        int pageSize = 20)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 20);

        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null)
            return OperationResult<TeacherGrowthStudentPageDto>.Failure("No current academic year found", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;

        if (classId.HasValue && subjectId.HasValue)
        {
            var assignment = await _unitOfWork.ClassSubjectTeachers.FirstOrDefaultAsync(cst =>
                cst.AcademicYearId == currentYear.Id &&
                cst.TeacherId == teacherId &&
                cst.ClassId == classId.Value &&
                cst.SubjectId == subjectId.Value &&
                !cst.IsDeleted);

            if (assignment == null)
                return OperationResult<TeacherGrowthStudentPageDto>.Failure("Teacher assignment not found", 404);

            var rows = await BuildTeacherGrowthStudentRowsAsync(assignment, resolvedTerm);
            var card = await BuildTeacherGrowthCardAsync(assignment, resolvedTerm);
            var result = new TeacherGrowthStudentPageDto
            {
                Summary = card,
                Students = new PagedResult<TeacherGrowthStudentDto>
                {
                    Items = SortAndPageRows(rows, page, pageSize),
                    TotalCount = rows.Count,
                    Page = page,
                    PageSize = pageSize
                }
            };
            return OperationResult<TeacherGrowthStudentPageDto>.Success(result);
        }

        var assignments = (await _unitOfWork.ClassSubjectTeachers.FindAsync(cst =>
            cst.AcademicYearId == currentYear.Id &&
            cst.TeacherId == teacherId &&
            !cst.IsDeleted)).ToList();

        if (assignments.Count == 0)
            return OperationResult<TeacherGrowthStudentPageDto>.Failure("Teacher has no assignments", 404);

        var allRows = new List<TeacherGrowthStudentDto>();
        foreach (var assignment in assignments)
        {
            var assignmentRows = await BuildTeacherGrowthStudentRowsAsync(assignment, resolvedTerm);
            allRows.AddRange(assignmentRows);
        }

        var mergedRows = DeduplicateStudentRows(allRows);
        var teacher = await _unitOfWork.Users.GetByIdAsync(teacherId);
        var combinedCard = BuildCombinedTeacherCard(teacherId, teacher, mergedRows);

        var result2 = new TeacherGrowthStudentPageDto
        {
            Summary = combinedCard,
            Students = new PagedResult<TeacherGrowthStudentDto>
            {
                Items = SortAndPageRows(mergedRows, page, pageSize),
                TotalCount = mergedRows.Count,
                Page = page,
                PageSize = pageSize
            }
        };
        return OperationResult<TeacherGrowthStudentPageDto>.Success(result2);
    }

    public async Task<OperationResult<List<StudentGrowthWeekDetailDto>>> GetStudentGrowthWeeksAsync(
        int studentId,
        int? classId = null,
        int? subjectId = null,
        int? teacherId = null,
        AcademicTerm? term = null)
    {
        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null)
            return OperationResult<List<StudentGrowthWeekDetailDto>>.Failure("لا توجد سنة دراسية حالية", 404);

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;

        var enrollments = (await _unitOfWork.StudentEnrollments.FindAsync(e =>
            e.StudentId == studentId &&
            e.AcademicYearId == currentYear.Id &&
            e.LeftAt == null &&
            !e.IsDeleted)).ToList();

        if (enrollments.Count == 0)
            return OperationResult<List<StudentGrowthWeekDetailDto>>.Failure("الطالب غير موجود في هذا الفصل", 404);

        List<int> enrollmentIds;
        if (classId.HasValue)
            enrollmentIds = enrollments.Where(e => e.ClassId == classId.Value).Select(e => e.Id).ToList();
        else
            enrollmentIds = enrollments.Select(e => e.Id).ToList();

        if (enrollmentIds.Count == 0)
            return OperationResult<List<StudentGrowthWeekDetailDto>>.Failure("الطالب غير موجود في هذا الفصل", 404);

        var (startDate, endDate) = GetTermDateRange(currentYear, resolvedTerm);
        var periods = (await _unitOfWork.EvaluationPeriods
            .FindAsync(p => p.StartDate >= startDate && p.EndDate <= endDate && !p.IsDeleted))
            .OrderBy(p => p.OrderNum)
            .ToList();

        // Resolve which subjectIds to filter by
        HashSet<int>? filterSubjectIds = null;
        if (subjectId.HasValue)
        {
            filterSubjectIds = new HashSet<int> { subjectId.Value };
        }
        else if (teacherId.HasValue)
        {
            var classIds = enrollments.Select(e => e.ClassId).Distinct().ToList();
            var teacherSubjects = (await _unitOfWork.ClassSubjectTeachers.FindAsync(cst =>
                cst.AcademicYearId == currentYear.Id &&
                cst.TeacherId == teacherId.Value &&
                classIds.Contains(cst.ClassId) &&
                !cst.IsDeleted)).ToList();
            var tsIds = teacherSubjects.Select(t => t.SubjectId).Distinct().ToHashSet();
            if (tsIds.Count > 0)
                filterSubjectIds = tsIds;
        }

        var details = new List<StudentGrowthWeekDetailDto>();

        foreach (var period in periods)
        {
            var evaluations = await _unitOfWork.StudentEvaluations
                .GetByPeriodAndEnrollmentsAsync(period.Id, enrollmentIds);

            IEnumerable<StudentEvaluation> validEvals = evaluations
                .Where(e => e.Score.HasValue);

            if (filterSubjectIds != null)
            {
                validEvals = validEvals
                    .Where(e => filterSubjectIds.Contains(e.EvaluationItem.Template.SubjectId));
            }

            var list = validEvals.ToList();
            if (list.Count == 0) continue;

            var max = list.Sum(e => (double)e.EvaluationItem.MaxScore);
            var score = list.Sum(e => (double)(e.Score ?? 0));

            details.Add(new StudentGrowthWeekDetailDto
            {
                PeriodName = period.Name,
                OrderNum = period.OrderNum,
                Score = Math.Round(score, 1),
                MaxScore = Math.Round(max, 1),
                Percentage = Math.Round(max > 0 ? score / max * 100 : 0, 1)
            });
        }

        // Fix: determine half based on actually evaluated periods, not all configured periods
        if (details.Count > 0)
        {
            var evaluatedOrders = details.Select(d => d.OrderNum).Distinct().OrderBy(o => o).ToList();
            var splitIndex = Math.Max(1, evaluatedOrders.Count / 2);
            var firstOrders = evaluatedOrders.Take(splitIndex).ToHashSet();
            foreach (var detail in details)
            {
                detail.IsFirstHalf = firstOrders.Contains(detail.OrderNum);
            }
        }

        return OperationResult<List<StudentGrowthWeekDetailDto>>.Success(details);
    }

    // ═══════════════════════════════════════════════════════════
    //  Shared pre‑fetch / common helpers
    // ═══════════════════════════════════════════════════════════

    private async Task<(AcademicYear? Year, AcademicTerm? Term, List<ClassSubjectTeacher> Assignments, GrowthPreFetchData Cache)>
        LoadDashboardCommonAsync(AcademicTerm? term, int? teacherId, int? classId)
    {
        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null)
            return (null, null, new List<ClassSubjectTeacher>(), new GrowthPreFetchData());

        var resolvedTerm = term ?? (await _academicYearService.GetCurrentTermAsync()).Data;
        var assignments = (await _unitOfWork.ClassSubjectTeachers.FindAsync(cst =>
            cst.AcademicYearId == currentYear.Id &&
            !cst.IsDeleted &&
            (!teacherId.HasValue || cst.TeacherId == teacherId.Value) &&
            (!classId.HasValue || cst.ClassId == classId.Value))).ToList();

        var cache = await BuildGrowthPreFetchAsync(assignments, currentYear, resolvedTerm);

        return (currentYear, resolvedTerm, assignments, cache);
    }

    private async Task<(List<TeacherGrowthCardDto> Cards, int EvaluatedWeeks, int ConfiguredWeeks)>
        BuildDashboardCardsAsync(List<ClassSubjectTeacher> assignments, AcademicTerm? term, GrowthPreFetchData cache)
    {
        var cards = new List<TeacherGrowthCardDto>();
        foreach (var assignment in assignments)
        {
            var card = await BuildTeacherGrowthCardAsync(assignment, term, cache);
            if (card.EvaluatedWeeks > 0 || card.EvaluatedStudentsCount > 0)
                cards.Add(card);
        }

        var orderedCards = cards
            .OrderByDescending(c => c.GrowthRate)
            .ThenByDescending(c => c.ImprovedStudentsRate)
            .ToList();

        var evaluatedWeeks = orderedCards.Count > 0 ? orderedCards.Max(c => c.EvaluatedWeeks) : 0;
        var configuredWeeks = orderedCards.Count > 0 ? orderedCards.Max(c => c.TotalConfiguredWeeks) : 14;

        return (orderedCards, evaluatedWeeks, configuredWeeks);
    }

    private async Task<TeacherGrowthCardDto> BuildTeacherGrowthCardAsync(ClassSubjectTeacher assignment, AcademicTerm? term, GrowthPreFetchData? cache = null)
    {
        var teacher = cache?.Teachers.GetValueOrDefault(assignment.TeacherId) ?? await _unitOfWork.Users.GetByIdAsync(assignment.TeacherId);
        var subject = cache?.Subjects.GetValueOrDefault(assignment.SubjectId) ?? await _unitOfWork.Subjects.GetByIdAsync(assignment.SubjectId);
        var classEntity = cache?.Classes.GetValueOrDefault(assignment.ClassId) ?? await _unitOfWork.Classes.GetByIdAsync(assignment.ClassId);
        var gradeLevel = classEntity != null
            ? (cache?.GradeLevels.GetValueOrDefault(classEntity.GradeLevelId) ?? await _unitOfWork.GradeLevels.GetByIdAsync(classEntity.GradeLevelId))
            : null;

        var rows = await BuildTeacherGrowthStudentRowsAsync(assignment, term, cache);

        if (rows.Count == 0)
        {
            return new TeacherGrowthCardDto
            {
                TeacherId = assignment.TeacherId,
                TeacherName = teacher?.FullName ?? $"مدرس {assignment.TeacherId}",
                SubjectId = assignment.SubjectId,
                SubjectName = subject?.Name ?? "",
                ClassId = assignment.ClassId,
                ClassName = classEntity?.Name ?? "",
                GradeLevelName = gradeLevel?.Name ?? "",
                StudentsCount = 0,
                EvaluatedStudentsCount = 0,
                EvaluatedWeeks = 0,
                TotalConfiguredWeeks = 14,
                ImprovedCount = 0,
                DeclinedCount = 0,
                StableCount = 0
            };
        }

        var evaluated = rows.Where(r => r.EvaluatedWeeks > 0).ToList();
        var evaluatedCount = evaluated.Count;
        var firstAvg = evaluatedCount > 0 ? evaluated.Average(r => r.FirstHalfAverage) : 0;
        var secondAvg = evaluatedCount > 0 ? evaluated.Average(r => r.SecondHalfAverage) : 0;
        var improved = evaluated.Count(r => r.Status == "improved");
        var declined = evaluated.Count(r => r.Status == "declined");
        var stable = Math.Max(evaluatedCount - improved - declined, 0);
        var growthRate = evaluatedCount > 0 ? (double)improved / evaluatedCount * 100 : 0;
        var declinedRate = evaluatedCount > 0 ? (double)declined / evaluatedCount * 100 : 0;
        var configuredWeeks = 14;

        return new TeacherGrowthCardDto
        {
            TeacherId = assignment.TeacherId,
            TeacherName = teacher?.FullName ?? $"مدرس {assignment.TeacherId}",
            SubjectId = assignment.SubjectId,
            SubjectName = subject?.Name ?? "",
            ClassId = assignment.ClassId,
            ClassName = classEntity?.Name ?? "",
            GradeLevelName = gradeLevel?.Name ?? "",
            StudentsCount = rows.Count,
            EvaluatedStudentsCount = evaluatedCount,
            EvaluatedWeeks = evaluatedCount > 0 ? evaluated.Max(r => r.EvaluatedWeeks) : 0,
            TotalConfiguredWeeks = configuredWeeks,
            FirstHalfAverage = Math.Round(firstAvg, 1),
            SecondHalfAverage = Math.Round(secondAvg, 1),
            AverageChange = Math.Round(secondAvg - firstAvg, 1),
            GrowthRate = Math.Round(growthRate, 1),
            ImprovedStudentsRate = Math.Round(growthRate, 1),
            DeclinedStudentsRate = Math.Round(declinedRate, 1),
            StableStudentsRate = Math.Round(evaluatedCount > 0 ? (double)stable / evaluatedCount * 100 : 0, 1),
            ExamGrowthRate = 0,
            Momentum = secondAvg - firstAvg >= 4 ? "up" : secondAvg - firstAvg <= -4 ? "down" : "stable",
            RiskLevel = declinedRate >= 35 ? "critical" : declinedRate >= 20 ? "watch" : "healthy",
            ImprovedCount = improved,
            DeclinedCount = declined,
            StableCount = stable
        };
    }

    private async Task<List<TeacherGrowthStudentDto>> BuildTeacherGrowthStudentRowsAsync(
        ClassSubjectTeacher assignment, AcademicTerm? term, GrowthPreFetchData? cache = null)
    {
        List<StudentEnrollment> enrollments;
        List<EvaluationPeriod> periods;

        if (cache != null)
        {
            enrollments = cache.AllEnrollments.Where(e => e.ClassId == assignment.ClassId).ToList();
            periods = cache.Periods;
        }
        else
        {
            enrollments = (await _unitOfWork.StudentEnrollments.FindAsync(e =>
                e.ClassId == assignment.ClassId &&
                e.AcademicYearId == assignment.AcademicYearId &&
                e.LeftAt == null &&
                !e.IsDeleted)).ToList();
            periods = await GetTeacherGrowthPeriodsAsync(assignment.AcademicYearId, term);
        }

        if (enrollments.Count == 0) return new List<TeacherGrowthStudentDto>();
        if (periods.Count == 0) return await BuildEmptyGrowthRowsAsync(enrollments, cache);

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var scoresByEnrollment = enrollmentIds.ToDictionary(id => id, _ => new List<(int OrderNum, double Score)>());

        if (cache != null)
        {
            foreach (var period in periods)
            {
                var key = (assignment.ClassId, period.Id, assignment.SubjectId);
                if (cache.EvalCache.TryGetValue(key, out var entries))
                {
                    foreach (var (enrollmentId, percent, _, _) in entries)
                    {
                        if (scoresByEnrollment.ContainsKey(enrollmentId))
                            scoresByEnrollment[enrollmentId].Add((period.OrderNum, percent));
                    }
                }
            }
        }
        else
        {
            foreach (var period in periods)
            {
                var evaluations = await _unitOfWork.StudentEvaluations.GetByPeriodAndEnrollmentsAsync(period.Id, enrollmentIds);
                var periodScores = evaluations
                    .Where(e => e.Score.HasValue && e.EvaluationItem.Template.SubjectId == assignment.SubjectId)
                    .GroupBy(e => e.EnrollmentId)
                    .Select(g =>
                    {
                        var max = g.Sum(e => (double)e.EvaluationItem.MaxScore);
                        var score = g.Sum(e => (double)(e.Score ?? 0));
                        return new { EnrollmentId = g.Key, Percent = max > 0 ? score / max * 100 : 0 };
                    });

                foreach (var s in periodScores)
                    scoresByEnrollment[s.EnrollmentId].Add((period.OrderNum, s.Percent));
            }
        }

        var evaluatedOrders = scoresByEnrollment.Values
            .SelectMany(s => s.Select(x => x.OrderNum))
            .Distinct()
            .OrderBy(o => o)
            .ToList();
        var splitIndex = Math.Max(1, evaluatedOrders.Count / 2);
        var firstOrders = evaluatedOrders.Take(splitIndex).ToHashSet();
        var secondOrders = evaluatedOrders.Skip(splitIndex).ToHashSet();
        if (secondOrders.Count == 0)
            secondOrders = evaluatedOrders.Skip(Math.Max(evaluatedOrders.Count - 1, 0)).ToHashSet();

        var rows = new List<TeacherGrowthStudentDto>();
        foreach (var enrollment in enrollments)
        {
            string studentName;
            if (cache != null)
                studentName = cache.Students.GetValueOrDefault(enrollment.StudentId)?.FullName ?? $"Student {enrollment.StudentId}";
            else
            {
                var student = await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId);
                studentName = student?.FullName ?? $"Student {enrollment.StudentId}";
            }
            var scores = scoresByEnrollment[enrollment.Id];
            var firstScores = scores.Where(s => firstOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var secondScores = scores.Where(s => secondOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var firstAvg = firstScores.Count > 0 ? firstScores.Average() : 0;
            var secondAvg = secondScores.Count > 0 ? secondScores.Average() : firstAvg;
            var change = secondAvg - firstAvg;

            rows.Add(new TeacherGrowthStudentDto
            {
                StudentId = enrollment.StudentId,
                StudentName = studentName,
                FirstHalfAverage = Math.Round(firstAvg, 1),
                SecondHalfAverage = Math.Round(secondAvg, 1),
                Change = Math.Round(change, 1),
                Status = Math.Round(change, 1) > 0 ? "improved" : Math.Round(change, 1) < 0 ? "declined" : "stable",
                EvaluatedWeeks = scores.Select(s => s.OrderNum).Distinct().Count()
            });
        }

        return rows;
    }

    private async Task<List<TeacherGrowthStudentDto>> BuildEmptyGrowthRowsAsync(
        List<StudentEnrollment> enrollments, GrowthPreFetchData? cache = null)
    {
        var rows = new List<TeacherGrowthStudentDto>();
        foreach (var enrollment in enrollments)
        {
            string studentName;
            if (cache != null)
                studentName = cache.Students.GetValueOrDefault(enrollment.StudentId)?.FullName ?? $"Student {enrollment.StudentId}";
            else
            {
                var student = await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId);
                studentName = student?.FullName ?? $"Student {enrollment.StudentId}";
            }
            rows.Add(new TeacherGrowthStudentDto
            {
                StudentId = enrollment.StudentId,
                StudentName = studentName,
                Status = "stable"
            });
        }
        return rows;
    }

    private async Task<List<EvaluationPeriod>> GetTeacherGrowthPeriodsAsync(int academicYearId, AcademicTerm? term)
    {
        var year = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId);
        var (startDate, endDate) = GetTermDateRange(year, term);
        return (await _unitOfWork.EvaluationPeriods
            .FindAsync(p => p.StartDate >= startDate && p.EndDate <= endDate && !p.IsDeleted))
            .OrderBy(p => p.OrderNum)
            .ToList();
    }

    private async Task<List<TeacherGrowthWeekDto>> BuildTeacherGrowthTrendAsync(
        List<ClassSubjectTeacher> assignments,
        AcademicYear currentYear,
        AcademicTerm? term,
        GrowthPreFetchData? cache = null)
    {
        List<EvaluationPeriod> periods;
        if (cache != null)
            periods = cache.Periods;
        else
            periods = await GetTeacherGrowthPeriodsAsync(currentYear.Id, term);

        var assignmentKeys = assignments.Select(a => new { a.ClassId, a.SubjectId }).Distinct().ToList();
        var validSubjectIds = assignmentKeys.Select(a => a.SubjectId).ToHashSet();
        var trend = new List<TeacherGrowthWeekDto>();

        if (cache != null)
        {
            foreach (var period in periods)
            {
                var allScores = cache.EvalCache
                    .Where(kvp => kvp.Key.PeriodId == period.Id && validSubjectIds.Contains(kvp.Key.SubjectId))
                    .SelectMany(kvp => kvp.Value.Select(e => e.Percent))
                    .ToList();

                trend.Add(new TeacherGrowthWeekDto
                {
                    PeriodId = period.Id,
                    Label = period.Name,
                    OrderNum = period.OrderNum,
                    AverageScore = Math.Round(allScores.Count > 0 ? allScores.Average() : 0, 1),
                    EvaluationsCount = allScores.Count
                });
            }
        }
        else
        {
            var classIds = assignmentKeys.Select(a => a.ClassId).Distinct().ToList();
            var enrollments = (await _unitOfWork.StudentEnrollments.FindAsync(e =>
                classIds.Contains(e.ClassId) &&
                e.AcademicYearId == currentYear.Id &&
                e.LeftAt == null &&
                !e.IsDeleted)).ToList();
            var enrollmentIds = enrollments.Select(e => e.Id).ToList();

            foreach (var period in periods)
            {
                var evaluations = await _unitOfWork.StudentEvaluations.GetByPeriodAndEnrollmentsAsync(period.Id, enrollmentIds);
                var validEvaluations = evaluations
                    .Where(e => e.Score.HasValue && validSubjectIds.Contains(e.EvaluationItem.Template.SubjectId))
                    .ToList();
                var max = validEvaluations.Sum(e => (double)e.EvaluationItem.MaxScore);
                var score = validEvaluations.Sum(e => (double)(e.Score ?? 0));

                trend.Add(new TeacherGrowthWeekDto
                {
                    PeriodId = period.Id,
                    Label = period.Name,
                    OrderNum = period.OrderNum,
                    AverageScore = Math.Round(max > 0 ? score / max * 100 : 0, 1),
                    EvaluationsCount = validEvaluations.Count
                });
            }
        }

        return trend;
    }

    private List<TeacherWeeklyTrendDto> BuildTeachersWeeklyTrendFromCache(
        List<ClassSubjectTeacher> assignments,
        GrowthPreFetchData cache)
    {
        var periods = cache.Periods;
        var teacherGroups = assignments.GroupBy(a => a.TeacherId).ToList();
        if (teacherGroups.Count == 0 || periods.Count == 0)
            return new List<TeacherWeeklyTrendDto>();

        var result = new List<TeacherWeeklyTrendDto>();

        foreach (var group in teacherGroups)
        {
            var teacherId = group.Key;
            var teacher = cache.Teachers.GetValueOrDefault(teacherId);
            var subjectIds = group.Select(a => a.SubjectId).Distinct().ToHashSet();
            var subjects = cache.Subjects.Where(s => subjectIds.Contains(s.Key)).Select(s => s.Value.Name).Distinct();
            var combinedSubject = string.Join("، ", subjects);

            var groupClassIds = group.Select(a => a.ClassId).Distinct().ToHashSet();
            var groupEnrollmentIds = cache.AllEnrollments
                .Where(e => groupClassIds.Contains(e.ClassId))
                .Select(e => e.Id)
                .ToHashSet();

            if (groupEnrollmentIds.Count == 0) continue;

            var weeklyScores = new List<TeacherGrowthWeekDto>();
            foreach (var period in periods)
            {
                var periodScores = new List<double>();

                foreach (var classId in groupClassIds)
                {
                    foreach (var subjectId in subjectIds)
                    {
                        var key = (classId, period.Id, subjectId);
                        if (cache.EvalCache.TryGetValue(key, out var entries))
                        {
                            var matched = entries
                                .Where(e => groupEnrollmentIds.Contains(e.EnrollmentId))
                                .Select(e => e.Percent);
                            periodScores.AddRange(matched);
                        }
                    }
                }

                if (periodScores.Count == 0) continue;

                weeklyScores.Add(new TeacherGrowthWeekDto
                {
                    PeriodId = period.Id,
                    Label = period.Name,
                    OrderNum = period.OrderNum,
                    AverageScore = Math.Round(periodScores.Average(), 1),
                    EvaluationsCount = periodScores.Count
                });
            }

            if (weeklyScores.Count == 0) continue;

            var evaluatedOrders = weeklyScores.Select(w => w.OrderNum).Distinct()
                .OrderBy(o => o).ToList();
            var splitIndex = Math.Max(1, evaluatedOrders.Count / 2);
            var firstOrders = evaluatedOrders.Take(splitIndex).ToHashSet();

            var firstScores = weeklyScores
                .Where(w => firstOrders.Contains(w.OrderNum))
                .Select(w => w.AverageScore).ToList();
            var secondScores = weeklyScores
                .Where(w => !firstOrders.Contains(w.OrderNum))
                .Select(w => w.AverageScore).ToList();

            result.Add(new TeacherWeeklyTrendDto
            {
                TeacherId = teacherId,
                TeacherName = teacher?.FullName ?? $"مدرس {teacherId}",
                SubjectName = combinedSubject,
                FirstHalfAverage = firstScores.Count > 0 ? Math.Round(firstScores.Average(), 1) : 0,
                SecondHalfAverage = secondScores.Count > 0 ? Math.Round(secondScores.Average(), 1) : 0,
                WeeklyScores = weeklyScores
            });
        }

        return result;
    }

    private (int Improved, int Declined, int Stable) ComputeUniqueStudentGrowthCountsFromCache(
        List<ClassSubjectTeacher> assignments,
        GrowthPreFetchData cache)
    {
        var periods = cache.Periods;
        var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToHashSet();
        var enrollmentIds = cache.AllEnrollments.Select(e => e.Id).ToList();

        if (enrollmentIds.Count == 0) return (0, 0, 0);
        if (periods.Count == 0) return (enrollmentIds.Count, 0, 0);

        var scoresByEnrollment = enrollmentIds.ToDictionary(id => id, _ => new List<(int OrderNum, double Score)>());

        foreach (var period in periods)
        {
            foreach (var subjectId in subjectIds)
            {
                var relevantKeys = cache.EvalCache
                    .Where(kvp => kvp.Key.PeriodId == period.Id && kvp.Key.SubjectId == subjectId)
                    .SelectMany(kvp => kvp.Value);

                foreach (var (enrollmentId, percent, _, _) in relevantKeys)
                {
                    if (scoresByEnrollment.ContainsKey(enrollmentId))
                        scoresByEnrollment[enrollmentId].Add((period.OrderNum, percent));
                }
            }
        }

        var evaluatedOrders = scoresByEnrollment.Values
            .SelectMany(s => s.Select(x => x.OrderNum))
            .Distinct()
            .OrderBy(o => o)
            .ToList();
        var splitIndex = Math.Max(1, evaluatedOrders.Count / 2);
        var firstOrders = evaluatedOrders.Take(splitIndex).ToHashSet();
        var secondOrders = evaluatedOrders.Skip(splitIndex).ToHashSet();
        if (secondOrders.Count == 0)
            secondOrders = evaluatedOrders.Skip(Math.Max(evaluatedOrders.Count - 1, 0)).ToHashSet();

        int improved = 0, declined = 0, stable = 0;
        foreach (var enrollmentId in enrollmentIds)
        {
            var scores = scoresByEnrollment[enrollmentId];
            var firstScores = scores.Where(s => firstOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var secondScores = scores.Where(s => secondOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var firstAvg = firstScores.Count > 0 ? firstScores.Average() : 0;
            var secondAvg = secondScores.Count > 0 ? secondScores.Average() : firstAvg;
            var change = secondAvg - firstAvg;

            if (change > 0) improved++;
            else if (change < 0) declined++;
            else stable++;
        }

        return (improved, declined, stable);
    }

    private double ComputeAverageAbsChangeFromCache(
        List<ClassSubjectTeacher> assignments,
        GrowthPreFetchData cache)
    {
        var periods = cache.Periods;
        var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToHashSet();
        var allEnrollments = cache.AllEnrollments.ToList();
        if (allEnrollments.Count == 0 || periods.Count == 0) return 0;

        var scoresByEnrollment = allEnrollments.ToDictionary(e => e.Id, _ => new List<(int OrderNum, double Score)>());

        foreach (var period in periods)
        {
            foreach (var subjectId in subjectIds)
            {
                var relevantKeys = cache.EvalCache
                    .Where(kvp => kvp.Key.PeriodId == period.Id && kvp.Key.SubjectId == subjectId)
                    .SelectMany(kvp => kvp.Value);

                foreach (var (enrollmentId, percent, _, _) in relevantKeys)
                {
                    if (scoresByEnrollment.ContainsKey(enrollmentId))
                        scoresByEnrollment[enrollmentId].Add((period.OrderNum, percent));
                }
            }
        }

        var evaluatedOrders = scoresByEnrollment.Values
            .SelectMany(s => s.Select(x => x.OrderNum))
            .Distinct()
            .OrderBy(o => o)
            .ToList();
        var splitIndex = Math.Max(1, evaluatedOrders.Count / 2);
        var firstOrders = evaluatedOrders.Take(splitIndex).ToHashSet();
        var secondOrders = evaluatedOrders.Skip(splitIndex).ToHashSet();
        if (secondOrders.Count == 0)
            secondOrders = evaluatedOrders.Skip(Math.Max(evaluatedOrders.Count - 1, 0)).ToHashSet();

        double totalAbsChange = 0;
        int count = 0;

        foreach (var enrollment in allEnrollments)
        {
            var scores = scoresByEnrollment[enrollment.Id];
            if (scores.Count == 0) continue;

            var firstScores = scores.Where(s => firstOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var secondScores = scores.Where(s => secondOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var firstAvg = firstScores.Count > 0 ? firstScores.Average() : 0;
            var secondAvg = secondScores.Count > 0 ? secondScores.Average() : firstAvg;
            var change = secondAvg - firstAvg;

            totalAbsChange += Math.Abs(change);
            count++;
        }

        return count > 0 ? totalAbsChange / count : 0;
    }

    private double ComputeAverageChangeFromCache(
        List<ClassSubjectTeacher> assignments,
        GrowthPreFetchData cache)
    {
        var periods = cache.Periods;
        var subjectIds = assignments.Select(a => a.SubjectId).Distinct().ToHashSet();
        var allEnrollments = cache.AllEnrollments.ToList();
        if (allEnrollments.Count == 0 || periods.Count == 0) return 0;

        var scoresByEnrollment = allEnrollments.ToDictionary(e => e.Id, _ => new List<(int OrderNum, double Score)>());

        foreach (var period in periods)
        {
            foreach (var subjectId in subjectIds)
            {
                var relevantKeys = cache.EvalCache
                    .Where(kvp => kvp.Key.PeriodId == period.Id && kvp.Key.SubjectId == subjectId)
                    .SelectMany(kvp => kvp.Value);

                foreach (var (enrollmentId, percent, _, _) in relevantKeys)
                {
                    if (scoresByEnrollment.ContainsKey(enrollmentId))
                        scoresByEnrollment[enrollmentId].Add((period.OrderNum, percent));
                }
            }
        }

        var evaluatedOrders = scoresByEnrollment.Values
            .SelectMany(s => s.Select(x => x.OrderNum))
            .Distinct()
            .OrderBy(o => o)
            .ToList();
        var splitIndex = Math.Max(1, evaluatedOrders.Count / 2);
        var firstOrders = evaluatedOrders.Take(splitIndex).ToHashSet();
        var secondOrders = evaluatedOrders.Skip(splitIndex).ToHashSet();
        if (secondOrders.Count == 0)
            secondOrders = evaluatedOrders.Skip(Math.Max(evaluatedOrders.Count - 1, 0)).ToHashSet();

        double totalChange = 0;
        int count = 0;

        foreach (var enrollment in allEnrollments)
        {
            var scores = scoresByEnrollment[enrollment.Id];
            if (scores.Count == 0) continue;

            var firstScores = scores.Where(s => firstOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var secondScores = scores.Where(s => secondOrders.Contains(s.OrderNum)).Select(s => s.Score).ToList();
            var firstAvg = firstScores.Count > 0 ? firstScores.Average() : 0;
            var secondAvg = secondScores.Count > 0 ? secondScores.Average() : firstAvg;
            var change = secondAvg - firstAvg;

            totalChange += change;
            count++;
        }

        return count > 0 ? totalChange / count : 0;
    }

    private List<TeacherGrowthSignalDto> BuildTeacherGrowthSignals(
        List<TeacherGrowthCardDto> orderedCards,
        int evaluatedWeeks,
        int configuredWeeks)
    {
        var signals = new List<TeacherGrowthSignalDto>();

        // Best performer
        var best = orderedCards.OrderByDescending(c => c.GrowthRate).FirstOrDefault();
        if (best != null && best.EvaluatedStudentsCount > 0)
        {
            signals.Add(new TeacherGrowthSignalDto
            {
                Title = "أفضل أداء",
                Description = $"{best.TeacherName} — {best.ImprovedCount} من أصل {best.EvaluatedStudentsCount} طالب تحسنوا في مادة {best.SubjectName}.",
                Severity = "success",
                TeacherId = best.TeacherId,
                TeacherName = best.TeacherName
            });
        }

        // Needs early intervention
        var worst = orderedCards.OrderByDescending(c => c.DeclinedStudentsRate).FirstOrDefault();
        if (worst != null && worst.DeclinedCount > 0 && worst.DeclinedStudentsRate >= 20)
        {
            signals.Add(new TeacherGrowthSignalDto
            {
                Title = "يحتاج تدخل مبكر",
                Description = $"{worst.DeclinedCount} من أصل {worst.EvaluatedStudentsCount} طالب انخفض أداؤهم مع {worst.TeacherName}.",
                Severity = "danger",
                TeacherId = worst.TeacherId,
                TeacherName = worst.TeacherName
            });
        }

        // Evaluation progress
        if (evaluatedWeeks < configuredWeeks)
        {
            signals.Add(new TeacherGrowthSignalDto
            {
                Title = "تقدم التقييمات",
                Description = $"تم تقييم {evaluatedWeeks} من أصل {configuredWeeks} أسبوع.",
                Severity = "info"
            });
        }

        return signals;
    }

    // ── Helpers for aggregated (all-assignments) mode ──────────────────────

    private static List<TeacherGrowthStudentDto> SortAndPageRows(List<TeacherGrowthStudentDto> rows, int page, int pageSize)
    {
        return rows
            .OrderBy(r => r.Status == "improved" ? 0 : r.Status == "declined" ? 1 : 2)
            .ThenByDescending(r => r.Change)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
    }

    private static List<TeacherGrowthStudentDto> DeduplicateStudentRows(List<TeacherGrowthStudentDto> rows)
    {
        return rows
            .GroupBy(r => r.StudentId)
            .Select(g => g.OrderByDescending(r => r.EvaluatedWeeks).ThenByDescending(r => Math.Abs(r.Change)).First())
            .OrderByDescending(r => r.Change)
            .ToList();
    }

    private static TeacherGrowthCardDto BuildCombinedTeacherCard(
        int teacherId,
        User? teacher,
        List<TeacherGrowthStudentDto> rows)
    {
        var evaluated = rows.Where(r => r.EvaluatedWeeks > 0).ToList();
        var evaluatedCount = evaluated.Count;
        var totalCount = rows.Count;

        var firstAvg = evaluatedCount > 0 ? evaluated.Average(r => r.FirstHalfAverage) : 0;
        var secondAvg = evaluatedCount > 0 ? evaluated.Average(r => r.SecondHalfAverage) : 0;
        var improved = evaluated.Count(r => r.Status == "improved");
        var declined = evaluated.Count(r => r.Status == "declined");
        var stable = Math.Max(evaluatedCount - improved - declined, 0);
        var growthRate = evaluatedCount > 0 ? (double)improved / evaluatedCount * 100 : 0;
        var declinedRate = evaluatedCount > 0 ? (double)declined / evaluatedCount * 100 : 0;

        return new TeacherGrowthCardDto
        {
            TeacherId = teacherId,
            TeacherName = teacher?.FullName ?? $"مدرس {teacherId}",
            SubjectId = 0,
            SubjectName = "جميع المواد",
            ClassId = 0,
            ClassName = "جميع الفصول",
            GradeLevelName = "",
            StudentsCount = totalCount,
            EvaluatedStudentsCount = evaluatedCount,
            EvaluatedWeeks = evaluatedCount > 0 ? evaluated.Max(r => r.EvaluatedWeeks) : 0,
            TotalConfiguredWeeks = 14,
            FirstHalfAverage = Math.Round(firstAvg, 1),
            SecondHalfAverage = Math.Round(secondAvg, 1),
            AverageChange = Math.Round(secondAvg - firstAvg, 1),
            GrowthRate = Math.Round(growthRate, 1),
            ImprovedStudentsRate = Math.Round(growthRate, 1),
            DeclinedStudentsRate = Math.Round(declinedRate, 1),
            StableStudentsRate = Math.Round(evaluatedCount > 0 ? (double)stable / evaluatedCount * 100 : 0, 1),
            ExamGrowthRate = 0,
            Momentum = secondAvg - firstAvg >= 4 ? "up" : secondAvg - firstAvg <= -4 ? "down" : "stable",
            RiskLevel = declinedRate >= 35 ? "critical" : declinedRate >= 20 ? "watch" : "healthy",
            ImprovedCount = improved,
            DeclinedCount = declined,
            StableCount = stable
        };
    }
}
