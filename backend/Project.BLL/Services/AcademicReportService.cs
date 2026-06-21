using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.DTOs.Reports;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class AcademicReportService : IAcademicReportService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AcademicReportService> _logger;

    private static readonly string[] MonthNames =
        ["يناير", "فبراير", "مارس", "إبريل", "مايو", "يونيو",
         "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"];

    public AcademicReportService(IUnitOfWork unitOfWork, ILogger<AcademicReportService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<OperationResult<AcademicReportDto>> GetAcademicReportAsync(
        int classId, AcademicTerm term, int subjectId, int? gradeLevelId = null)
    {
        var subject = await _unitOfWork.Subjects.GetByIdAsync(subjectId);
        if (subject is null || subject.IsDeleted)
            return OperationResult<AcademicReportDto>.Failure("المادة غير موجودة");
        var subjectName = subject.Name;

        int academicYearId;
        IReadOnlyList<FinalGrade> grades;
        string displayClassName;

        if (gradeLevelId.HasValue)
        {
            // ── Grade-level aggregation ──
            var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(gradeLevelId.Value);
            if (gradeLevel is null || gradeLevel.IsDeleted)
                return OperationResult<AcademicReportDto>.Failure("المرحلة الدراسية غير موجودة");

            // Use the selected class to determine academic year
            var refClass = await _unitOfWork.Classes.GetByIdWithIncludesAsync(classId);
            if (refClass is null || refClass.IsDeleted)
                return OperationResult<AcademicReportDto>.Failure("الفصل المرجعي غير موجود");

            academicYearId = refClass.AcademicYearId;

            var allClassesOfGrade = await _unitOfWork.Classes
                .GetByGradeLevelAndYearAsync(gradeLevelId.Value, academicYearId);

            var allGrades = new List<FinalGrade>();
            foreach (var cls in allClassesOfGrade)
            {
                var clsGrades = await _unitOfWork.FinalGrades
                    .GetByClassIdAsync(cls.Id, term, subjectId);
                allGrades.AddRange(clsGrades);
            }
            grades = allGrades;
            displayClassName = gradeLevel.Name;
        }
        else
        {
            // ── Single-class report ──
            var classEntity = await _unitOfWork.Classes.GetByIdWithIncludesAsync(classId);
            if (classEntity is null || classEntity.IsDeleted)
                return OperationResult<AcademicReportDto>.Failure("الفصل غير موجود");

            academicYearId = classEntity.AcademicYearId;
            grades = await _unitOfWork.FinalGrades
                .GetByClassIdAsync(classId, term, subjectId);
            displayClassName = classEntity.GradeLevel != null
                ? $"{classEntity.GradeLevel.Name} - {classEntity.Name}"
                : classEntity.Name;
        }

        if (grades.Count == 0)
        {
            var emptyDto = new AcademicReportDto
            {
                ClassName = displayClassName,
                TermLabel = term == AcademicTerm.FirstSemester ? "الفصل الدراسي الأول" : "الفصل الدراسي الثاني",
                SubjectName = subjectName,
                StudentCount = 0,
            };
            return OperationResult<AcademicReportDto>.Success(emptyDto, "لا توجد درجات مسجلة");
        }

        var enrollmentIds = grades
            .Select(g => g.EnrollmentId)
            .Distinct()
            .ToList();

        // ── 2. Weekly periods ────────────────────────────────
        var weeklyPeriods = (await _unitOfWork.EvaluationPeriods
                .GetWeeksByYearAsync(academicYearId))
            .Where(p => p.SemesterNumber is null || p.SemesterNumber == (int)term)
            .OrderBy(p => p.OrderNum)
            .ToList();

        // ── 3. Student evaluations per period ────────────────
        var evalMap = new Dictionary<int, Dictionary<int, WeeklyScoreRaw>>();

        foreach (var period in weeklyPeriods)
        {
            var evaluations = await _unitOfWork.StudentEvaluations
                .GetByPeriodAndEnrollmentsAsync(period.Id, enrollmentIds);

            var grouped = evaluations
                .Where(e => e.EvaluationItem?.Template?.Subject?.Name == subjectName)
                .GroupBy(e => e.EnrollmentId);

            foreach (var g in grouped)
            {
                if (!evalMap.ContainsKey(g.Key))
                    evalMap[g.Key] = new Dictionary<int, WeeklyScoreRaw>();

                var totalScore = g.Sum(e => e.Score ?? 0m);
                var maxScore = g.Sum(e =>
                    (e.EvaluationItem?.MaxScore ?? 0) * (e.EvaluationItem?.Weight ?? 1));
                var avg = maxScore > 0
                    ? Math.Round((double)(totalScore / maxScore * 100), 1)
                    : 0.0;

                evalMap[g.Key][period.Id] = new WeeklyScoreRaw
                {
                    Avg = avg,
                    Max = 100,
                    RawScore = Math.Round((double)totalScore, 1),
                    RawMax = (double)maxScore
                };
            }
        }

        // ── 4. Compute per-subject evaluation average from evalMap (correct) ──
        var enrollmentEvalAvg = new Dictionary<int, double>();
        foreach (var eid in enrollmentIds)
        {
            if (evalMap.TryGetValue(eid, out var weeks) && weeks.Count > 0)
            {
                var avgRaw = weeks.Values.Select(w => w.RawScore).Average();
                enrollmentEvalAvg[eid] = Math.Round(avgRaw, 1);
            }
        }

        // ── 5. Build student rows ────────────────────────────
        var studentNameMap = new Dictionary<int, string>();
        var rows = new List<StudentReportRowDto>();

        foreach (var grade in grades)
        {
            var enrollmentId = grade.EnrollmentId;
            var studentName = grade.Enrollment?.Student?.FullName ?? "طالب";
            studentNameMap[enrollmentId] = studentName;

            // Use per-subject evaluation average instead of the inflated PeriodAvgScore
            var correctPeriodAvg = enrollmentEvalAvg.GetValueOrDefault(enrollmentId, 0);
            var correctWrittenTotal = correctPeriodAvg + (double)grade.Assessment1Score + (double)grade.Assessment2Score;
            var correctTotal = correctWrittenTotal + (double)grade.FinalExamScore;
            var correctPct = grade.MaxTotal > 0
                ? Math.Round(correctTotal / (double)grade.MaxTotal * 100)
                : 0;

            var weeklyScores = weeklyPeriods.Select(p =>
            {
                var raw = evalMap.GetValueOrDefault(enrollmentId)
                    ?.GetValueOrDefault(p.Id);
                return new WeeklyScoreDto
                {
                    PeriodId = p.Id,
                    PeriodName = p.Name,
                    Avg = raw?.Avg ?? 0,
                    Max = raw?.Max ?? 100,
                    RawScore = raw?.RawScore ?? 0,
                    RawMax = raw?.RawMax ?? 0
                };
            }).ToList();

            rows.Add(new StudentReportRowDto
            {
                EnrollmentId = enrollmentId,
                Name = studentName,
                WeeklyScores = weeklyScores,
                Assessment1 = (double)grade.Assessment1Score,
                Assessment2 = (double)grade.Assessment2Score,
                TotalMonthly = Math.Round(correctWrittenTotal, 1),
                FinalTotal = Math.Round(correctTotal, 1),
                MaxTotal = (double)grade.MaxTotal,
                Percentage = correctPct
            });
        }

        // ── 6. Monthly exam data ─────────────────────────────
        // Fetch for all enrollment IDs directly rather than per-class
        var monthlyExams = await BuildMonthlyExams(enrollmentIds, studentNameMap, term);

        // ── 7. Summary stats ─────────────────────────────────
        var n = rows.Count;
        var avgPct = n > 0 ? Math.Round(rows.Average(r => r.Percentage)) : 0;
        var avgA1 = n > 0 ? Math.Round(rows.Average(r => r.Assessment1), 1) : 0.0;
        var avgA2 = n > 0 ? Math.Round(rows.Average(r => r.Assessment2), 1) : 0.0;
        var avgFinal = n > 0 ? Math.Round(rows.Average(r => r.FinalTotal), 1) : 0.0;

        // ── 8. Month groups ──────────────────────────────────
        var monthGroups = weeklyPeriods
            .GroupBy(p => p.MonthName ?? "غير محدد")
            .Select(g => new MonthGroupDto
            {
                MonthName = g.Key,
                Periods = g.Select(p => new PeriodDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    MonthName = p.MonthName,
                    OrderNum = p.OrderNum
                }).ToList()
            })
            .ToList();

        // ── 9. Quick lists ───────────────────────────────────
        var topStudents = rows
            .OrderByDescending(r => r.Percentage)
            .Take(10)
            .Select(r => new TopStudentDto { Name = r.Name, Percentage = r.Percentage })
            .ToList();

        var needingSupport = rows
            .Where(r => r.Percentage < 50)
            .OrderBy(r => r.Percentage)
            .Select(r => new StudentSummaryDto { Name = r.Name, Percentage = r.Percentage })
            .ToList();

        var result = new AcademicReportDto
        {
            ClassName = displayClassName,
            TermLabel = term == AcademicTerm.FirstSemester ? "الفصل الدراسي الأول" : "الفصل الدراسي الثاني",
            SubjectName = subjectName,
            StudentCount = rows.Count,
            AvgPercent = avgPct,
            AvgAssessment1 = avgA1,
            AvgAssessment2 = avgA2,
            AvgFinal = avgFinal,
            WeeklyPeriods = weeklyPeriods.Select(p => new PeriodDto
            {
                Id = p.Id,
                Name = p.Name,
                MonthName = p.MonthName,
                OrderNum = p.OrderNum
            }).ToList(),
            MonthGroups = monthGroups,
            Students = rows,
            MonthlyExams = monthlyExams,
            TopStudents = topStudents,
            StudentsNeedingSupport = needingSupport,
        };

        return OperationResult<AcademicReportDto>.Success(result, "تم إنشاء التقرير الدراسي بنجاح");
    }

    private async Task<List<MonthlyExamEntryDto>> BuildMonthlyExams(
        List<int> enrollmentIds,
        Dictionary<int, string> studentNameMap,
        AcademicTerm term)
    {
        // Fetch all periodic assessments for these enrollments in one go
        var allAssessments = new List<PeriodicAssessment>();
        foreach (var eid in enrollmentIds)
        {
            var eAssessments = await _unitOfWork.PeriodicAssessments
                .GetByEnrollmentIdAsync(eid);
            allAssessments.AddRange(eAssessments);
        }

        // Filter by term
        allAssessments = allAssessments
            .Where(a => a.Term == null || a.Term == term)
            .ToList();

        var exam1Map = allAssessments
            .Where(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam1
                     || a.AssessmentType == PeriodicAssessmentType.InitialAssessment)
            .ToLookup(a => a.EnrollmentId);
        var exam2Map = allAssessments
            .Where(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam2
                     || a.AssessmentType == PeriodicAssessmentType.FinalAssessment)
            .ToLookup(a => a.EnrollmentId);
        var semesterMap = allAssessments
            .Where(a => a.AssessmentType == PeriodicAssessmentType.SemesterExam)
            .ToLookup(a => a.EnrollmentId);

        return enrollmentIds.Select(eid =>
        {
            var e1 = exam1Map[eid].FirstOrDefault();
            var e2 = exam2Map[eid].FirstOrDefault();
            var sem = semesterMap[eid].FirstOrDefault();

            return new MonthlyExamEntryDto
            {
                EnrollmentId = eid,
                StudentName = studentNameMap.GetValueOrDefault(eid) ?? "",
                Exam1Score = (double)(e1?.Score ?? 0),
                Exam1Max = (double)(e1?.MaxScore ?? 0),
                Exam1Month = GetMonthNameFromDate(e1?.AssessmentDate),
                Exam2Score = (double)(e2?.Score ?? 0),
                Exam2Max = (double)(e2?.MaxScore ?? 0),
                Exam2Month = GetMonthNameFromDate(e2?.AssessmentDate),
                SemesterScore = (double)(sem?.Score ?? 0),
                SemesterMax = (double)(sem?.MaxScore ?? 0),
            };
        }).ToList();
    }

    private string GetMonthNameFromDate(DateOnly? date)
    {
        if (!date.HasValue) return "";
        var idx = date.Value.Month - 1;
        return idx >= 0 && idx < MonthNames.Length ? MonthNames[idx] : "";
    }

    private class WeeklyScoreRaw
    {
        public double Avg { get; set; }
        public double Max { get; set; }
        public double RawScore { get; set; }
        public double RawMax { get; set; }
    }
}
