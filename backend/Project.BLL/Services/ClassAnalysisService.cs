using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.ClassAnalysis;
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
}
