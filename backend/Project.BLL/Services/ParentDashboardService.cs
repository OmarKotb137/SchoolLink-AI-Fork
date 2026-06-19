using Common.Results;
using Microsoft.EntityFrameworkCore;
using Project.BLL.DTOs.Dashboard;
using Project.BLL.Interfaces;
using Project.DAL.Context;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class ParentDashboardService : IParentDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AppDbContext _context;
    private readonly IPeriodAverageService _periodAverageService;
    private readonly IExamService _examService;
    private readonly IResultVisibilityService _resultVisibilityService;

    public ParentDashboardService(
        IUnitOfWork unitOfWork,
        AppDbContext context,
        IPeriodAverageService periodAverageService,
        IExamService examService,
        IResultVisibilityService resultVisibilityService)
    {
        _unitOfWork = unitOfWork;
        _context = context;
        _periodAverageService = periodAverageService;
        _examService = examService;
        _resultVisibilityService = resultVisibilityService;
    }

    public async Task<OperationResult<ParentDashboardDto>> GetParentDashboardAsync(int parentId, int? termValue = null)
    {
        var dto = new ParentDashboardDto();

        var links = await _unitOfWork.ParentStudents.GetWithStudentDetailsByParentAsync(parentId);
        var activeLinks = links.Where(l => !l.IsDeleted && !l.Student.IsDeleted).ToList();

        foreach (var link in activeLinks)
        {
            var student = link.Student;
            var enrollment = student.Enrollments.FirstOrDefault(e => e.LeftAt == null);
            if (enrollment == null) continue;

            var className = enrollment.Class?.Name ?? "";
            var gradeName = enrollment.Class?.GradeLevel?.Name ?? "";

            // ── Detect current term, or use the one provided by the user ──
            var term = termValue.HasValue
                ? (AcademicTerm)termValue.Value
                : await DetectCurrentTermAsync(enrollment.Id);

            // ── Check if results are visible (admin setting) ─────────
            var academicYearId = enrollment.Class?.AcademicYearId;
            bool resultsVisible = true;
            if (academicYearId.HasValue && term.HasValue)
            {
                // If no setting exists, results are visible by default
                var existsResult = await _resultVisibilityService.ExistsSettingAsync(academicYearId.Value, term.Value);
                if (existsResult.IsSuccess && existsResult.Data)
                {
                    var visResult = await _resultVisibilityService.IsResultsVisibleAsync(academicYearId.Value, term.Value);
                    resultsVisible = visResult.IsSuccess && visResult.Data;
                }
            }

            // ── Absences ──────────────────────────────────────────
            var absences = await _unitOfWork.DailyAbsences
                .FindAsync(a => a.EnrollmentId == enrollment.Id && a.IsAbsent && !a.IsDeleted);
            var absCount = absences.Count;
            var excused = absences.Count(a => !string.IsNullOrWhiteSpace(a.Reason));
            var unexcused = absCount - excused;

            var totalDays = await _unitOfWork.DailyAbsences
                .FindAsync(a => a.EnrollmentId == enrollment.Id && !a.IsDeleted);
            var attendanceRate = totalDays.Count > 0
                ? Math.Round((double)(totalDays.Count - absCount) / totalDays.Count * 100, 1)
                : 100;

            // ── Assessments (periodic) ─────────────────────────────
            var assessments = await _unitOfWork.PeriodicAssessments
                .FindAsync(pa => pa.EnrollmentId == enrollment.Id);
            var last = assessments.OrderByDescending(a => a.AssessmentDate).FirstOrDefault();
            var lastScore = last != null ? $"{last.Score}/{last.MaxScore}" : "—";

            // Build a lookup: SubjectId → PeriodicAssessmentType → PeriodicAssessment
            // IMPORTANT: filter by current term so we don't mix semesters
            var subjectExamMap = assessments
                .Where(a => a.Term == term)
                .GroupBy(a => a.SubjectId ?? 0)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToLookup(a => a.AssessmentType));

            // ── Period Averages (weekly performance) ───────────────
            var allPeriodAvgs = await _unitOfWork.PeriodAverages
                .FindAsync(pa => pa.EnrollmentId == enrollment.Id);

            // Filter by current term
            var periodIdsForTerm = await GetTermPeriodIdsAsync(term);
            var periodAvgs = allPeriodAvgs
                .Where(pa => periodIdsForTerm == null || periodIdsForTerm.Contains(pa.PeriodId))
                .ToList();

            var totalPct = periodAvgs.Any()
                ? $"{Math.Round(periodAvgs.Average(a => (double)(a.AvgScore / a.MaxScore) * 100), 1)}%"
                : "—";

            var performance = periodAvgs.Any()
                ? (decimal)Math.Round(periodAvgs.Average(a => (double)(a.AvgScore / a.MaxScore) * 100), 1)
                : 0;

            // ── Weekly performances (for chart) ────────────────────
            var weekPerformances = new List<WeeklyPerformanceDto>();
            if (enrollment != null)
            {
                // Get weekly evaluation periods for the current term
                var semesterNumber = term.HasValue ? (int)term.Value : (int?)null;
                var weeklyPeriods = await _context.EvaluationPeriods
                    .Where(p =>
                        (!semesterNumber.HasValue || p.SemesterNumber == semesterNumber) &&
                        p.PeriodType == PeriodType.Weekly &&
                        !p.IsDeleted)
                    .OrderBy(p => p.Id)
                    .ToListAsync();

                // Get all student evaluations for this enrollment, filtered to weekly periods
                var weeklyPeriodIds = weeklyPeriods.Select(p => p.Id).ToList();
                var evaluations = await _context.StudentEvaluations
                    .Where(e =>
                        e.EnrollmentId == enrollment.Id &&
                        weeklyPeriodIds.Contains(e.PeriodId) &&
                        !e.IsDeleted)
                    .Include(e => e.EvaluationItem)
                    .ToListAsync();

                // Group by period and compute weighted average
                int weekNum = 1;
                var evalGroups = evaluations
                    .GroupBy(e => e.PeriodId)
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var group in evalGroups)
                {
                    var period = weeklyPeriods.First(p => p.Id == group.Key);
                    var totalWeighted = 0m;
                    var totalWeights = 0m;
                    foreach (var e in group)
                    {
                        var item = e.EvaluationItem;
                        if (item == null) continue;
                        var score = e.Score ?? 0;
                        totalWeighted += score * item.Weight;
                        totalWeights += item.MaxScore * item.Weight;
                    }
                    var avgPct = totalWeights > 0
                        ? Math.Round(totalWeighted / totalWeights * 100, 1)
                        : 0;

                    weekPerformances.Add(new WeeklyPerformanceDto
                    {
                        PeriodName = period.Name ?? $"الأسبوع {weekNum}",
                        WeekNumber = weekNum++,
                        AvgScore = avgPct,
                        MaxScore = 100,
                        TotalScore = totalWeighted,
                        TotalMaxScore = totalWeights
                    });
                }
            }

            // ── Subject performances + Monthly/Final exams per subject ──
            var subjectPerformances = new List<ChildSubjectDto>();
            var monthlyExams = new List<MonthlyExamResultDto>();
            var finalExams = new List<FinalExamResultDto>();
            if (enrollment != null)
            {
                var subjResult = await _periodAverageService.GetByEnrollmentGroupedBySubjectAsync(enrollment.Id, term);
                if (subjResult.IsSuccess && subjResult.Data != null)
                {
                    foreach (var group in subjResult.Data)
                    {
                        // Filter periods to current term only
                        var termPeriods = group.Periods
                            .Where(p => periodIdsForTerm == null || periodIdsForTerm.Contains(p.PeriodId))
                            .ToList();

                        // ── Latest period → subject performance (current) ──
                        var latest = termPeriods.OrderByDescending(p => p.PeriodId).FirstOrDefault();
                        if (latest != null)
                        {
                            subjectPerformances.Add(new ChildSubjectDto
                            {
                                SubjectName = group.SubjectName,
                                Score = latest.AvgScore > 0 ? Math.Round(latest.AvgScore / 100 * latest.MaxScore, 1) : 0,
                                MaxScore = latest.MaxScore
                            });
                        }

                        // ── Monthly periods (PeriodType=2) → monthly exams per subject ──
                        // Take only first 2 monthly periods (each subject has 2 monthly exams)
                        var monthlyPeriods = termPeriods
                            .Where(p => p.PeriodType == "2")
                            .OrderBy(p => p.PeriodId)
                            .Take(2)
                            .ToList();
                        int monthIdx = 1;
                        var subjLookup = subjectExamMap.GetValueOrDefault(group.SubjectId);
                        foreach (var mp in monthlyPeriods)
                        {
                            // Map month index to PeriodicAssessmentType
                            var examType = monthIdx == 1
                                ? PeriodicAssessmentType.MonthlyExam1
                                : PeriodicAssessmentType.MonthlyExam2;
                            var hasActualExam = subjLookup?.Contains(examType) == true;
                            var periodicAssessment = hasActualExam ? subjLookup![examType].First() : null;
                            var actualMaxScore = hasActualExam ? periodicAssessment!.MaxScore : mp.MaxScore;
                            var actualScore = hasActualExam ? periodicAssessment!.Score
                                : (mp.AvgScore > 0 ? Math.Round(mp.AvgScore / 100 * actualMaxScore, 1) : 0);

                            // Determine month name from actual assessment date if available
                            string monthName;
                            if (periodicAssessment?.AssessmentDate.HasValue == true)
                            {
                                monthName = periodicAssessment.AssessmentDate.Value.Month switch
                                {
                                    1 => "يناير",
                                    2 => "فبراير",
                                    3 => "مارس",
                                    4 => "إبريل",
                                    5 => "مايو",
                                    6 => "يونيو",
                                    7 => "يوليو",
                                    8 => "أغسطس",
                                    9 => "سبتمبر",
                                    10 => "أكتوبر",
                                    11 => "نوفمبر",
                                    12 => "ديسمبر",
                                    _ => $"شهر {monthIdx}"
                                };
                            }
                            else
                            {
                                // Fallback: use term-based naming
                                monthName = term == AcademicTerm.FirstSemester
                                    ? (monthIdx == 1 ? "مارس" : "إبريل")
                                    : (monthIdx == 1 ? "مايو" : "يونيو");
                            }

                            monthlyExams.Add(new MonthlyExamResultDto
                            {
                                SubjectName = group.SubjectName,
                                Title = $"امتحان شهر {monthName}",
                                Score = actualScore,
                                MaxScore = actualMaxScore
                            });
                            monthIdx++;
                        }

                        // ── Semester periods (PeriodType=3) → final exam per subject ──
                        var semesterPeriod = termPeriods
                            .FirstOrDefault(p => p.PeriodType == "3");
                        if (semesterPeriod != null)
                        {
                            var hasFinalExam = subjLookup?.Contains(PeriodicAssessmentType.SemesterExam) == true
                                            || subjLookup?.Contains(PeriodicAssessmentType.FinalAssessment) == true;
                            var semAssessment = hasFinalExam
                                ? (subjLookup![PeriodicAssessmentType.SemesterExam].FirstOrDefault()
                                   ?? subjLookup[PeriodicAssessmentType.FinalAssessment].FirstOrDefault())
                                : null;

                            decimal finalExamScore = 0;
                            decimal finalExamMaxScore = 0;

                            if (hasFinalExam && semAssessment != null)
                            {
                                finalExamScore = semAssessment.Score;
                                finalExamMaxScore = semAssessment.MaxScore;
                            }
                            else
                            {
                                // Fallback: try to get from FinalGrade (prefer the most complete record)
                                var finalGrade = await _context.FinalGrades
                                    .Where(fg => fg.EnrollmentId == enrollment.Id && fg.SubjectId == group.SubjectId && fg.Term == term && !fg.IsDeleted)
                                    .OrderByDescending(fg => fg.MaxTotal)
                                    .FirstOrDefaultAsync();
                                if (finalGrade != null && finalGrade.FinalExamScore > 0)
                                {
                                    finalExamScore = finalGrade.FinalExamScore;

                                    // Derive final exam max from MaxTotal formula:
                                    // MaxTotal = yearWorkMax + exam1Max + exam2Max + finalExamMax
                                    var yearWorkMax = monthlyPeriods.FirstOrDefault()?.MaxScore ?? semesterPeriod.MaxScore;
                                    var subjAssessments = subjectExamMap.GetValueOrDefault(group.SubjectId);
                                    var exam1Max = subjAssessments?.Contains(PeriodicAssessmentType.MonthlyExam1) == true
                                        ? subjAssessments![PeriodicAssessmentType.MonthlyExam1].First().MaxScore : 0;
                                    var exam2Max = subjAssessments?.Contains(PeriodicAssessmentType.MonthlyExam2) == true
                                        ? subjAssessments![PeriodicAssessmentType.MonthlyExam2].First().MaxScore : 0;
                                    finalExamMaxScore = finalGrade.MaxTotal - yearWorkMax - exam1Max - exam2Max;
                                    if (finalExamMaxScore <= 0)
                                        finalExamMaxScore = 30; // safe fallback if derivation fails

                                    hasFinalExam = true;
                                }
                            }

                            var actualMaxScore = hasFinalExam ? finalExamMaxScore : semesterPeriod.MaxScore;
                            var actualScore = hasFinalExam ? finalExamScore
                                : (semesterPeriod.AvgScore > 0 ? Math.Round(semesterPeriod.AvgScore / 100 * actualMaxScore, 1) : 0);

                            finalExams.Add(new FinalExamResultDto
                            {
                                SubjectName = group.SubjectName,
                                Title = "امتحان نهاية الترم",
                                Score = actualScore,
                                MaxScore = actualMaxScore
                            });
                        }
                    }
                }
            }

            // ── Upcoming exams ─────────────────────────────────────
            var upcomingExams = new List<ChildUpcomingExamDto>();
            if (enrollment.ClassId > 0 && enrollment.Class?.AcademicYearId > 0)
            {
                var yearId = enrollment.Class.AcademicYearId;
                var examResult = await _examService.GetUpcomingExamsAsync(enrollment.ClassId, yearId);
                if (examResult.IsSuccess && examResult.Data != null)
                {
                    upcomingExams = examResult.Data.Select(e => new ChildUpcomingExamDto
                    {
                        Title = e.Title,
                        SubjectName = e.SubjectName,
                        StartTime = e.StartTime,
                        TotalScore = e.TotalScore
                    }).Take(5).ToList();
                }
            }

            // ── Recommendations (from stored AIReports only) ───────
            string? recsText = null;
            var recSections = new List<RecommendationSectionDto>();
            try
            {
                var existingRecs = await _unitOfWork.AIReports
                    .FindAsync(r => r.StudentId == student.Id && r.ReportType == "Recommendations" && !r.IsDeleted);
                var latestRec = existingRecs.OrderByDescending(r => r.CreatedAt).FirstOrDefault();
                if (latestRec != null)
                {
                    recsText = latestRec.Content;
                    var contentLines = (latestRec.Content ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    RecommendationSectionDto? currentSection = null;
                    foreach (var rawLine in contentLines)
                    {
                        var line = rawLine.Trim();
                        if (line.Length == 0) continue;
                        if (line.StartsWith("**") && line.Contains("**", StringComparison.Ordinal))
                        {
                            var endIdx = line.LastIndexOf("**");
                            if (endIdx > 2)
                            {
                                currentSection = new RecommendationSectionDto
                                {
                                    Title = line[2..endIdx].Trim()
                                };
                                recSections.Add(currentSection);
                                var rest = line[(endIdx + 2)..].Trim();
                                if (rest.Length > 0) currentSection.Items.Add(rest);
                                continue;
                            }
                        }
                        if ((line.StartsWith('-') || line.StartsWith('•') || line.StartsWith('*')) && currentSection != null)
                        {
                            currentSection.Items.Add(line.TrimStart('-', '•', '*', ' ').Trim());
                        }
                    }
                }
            }
            catch { /* skip */ }

            var termName = term switch
            {
                AcademicTerm.FirstSemester => "الأول",
                AcademicTerm.SecondSemester => "الثاني",
                AcademicTerm.Final => "النهائي",
                _ => ""
            };

            // ── Hide only end-of-term exam scores if admin disabled visibility ─
            if (!resultsVisible)
            {
                finalExams.Clear();
            }

            dto.Children.Add(new ParentChildDto
            {
                Name = student.FullName,
                Grade = gradeName,
                Class = className,
                Performance = performance,
                Grades = new ChildGradesDto
                {
                    Last = lastScore,
                    Total = totalPct
                },
                Absences = absCount,
                AttendanceRate = attendanceRate,
                ExcusedAbsences = excused,
                UnexcusedAbsences = unexcused,
                SubjectPerformances = subjectPerformances,
                UpcomingExams = upcomingExams,
                WeeklyPerformances = weekPerformances,
                MonthlyExams = monthlyExams,
                FinalExams = finalExams,
                RecommendationsText = recsText,
                RecommendationSections = recSections,
                CurrentTermName = termName
            });

            dto.RecentActivities.Add($"تم تحديث درجات {student.FullName} في التقييم الأسبوعي");
        }

        dto.RecentActivities.AddRange(new[]
        {
            "تم تحديث جدول الأبناء للعام الدراسي",
            "متوفر تقرير الأداء الشهري"
        });

        return OperationResult<ParentDashboardDto>.Success(dto, "تم جلب بيانات dashboard ولي الأمر بنجاح");
    }

    /// <summary>
    /// Detect the current academic term for a given enrollment based on
    /// the most recent evaluation period that has data.
    /// </summary>
    private async Task<AcademicTerm?> DetectCurrentTermAsync(int enrollmentId)
    {
        var avgPeriods = await _unitOfWork.PeriodAverages
            .FindAsync(pa => pa.EnrollmentId == enrollmentId);
        if (!avgPeriods.Any()) return AcademicTerm.SecondSemester; // fallback

        var latestPeriodId = avgPeriods.OrderByDescending(pa => pa.PeriodId)
            .Select(pa => pa.PeriodId).First();

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(latestPeriodId);
        if (period == null) return AcademicTerm.SecondSemester;

        // Also check if there are periods with the same semester number
        var semesterPeriods = await _context.EvaluationPeriods
            .Where(p => p.SemesterNumber == period.SemesterNumber && !p.IsDeleted)
            .Select(p => p.Id)
            .ToListAsync();

        return period.SemesterNumber switch
        {
            1 => AcademicTerm.FirstSemester,
            2 => AcademicTerm.SecondSemester,
            _ => AcademicTerm.SecondSemester
        };
    }

    /// <summary>
    /// Get period IDs that belong to the given term (semester).
    /// </summary>
    private async Task<HashSet<int>?> GetTermPeriodIdsAsync(AcademicTerm? term)
    {
        if (!term.HasValue) return null; // all periods

        var semesterNumber = (int)term.Value;
        var periods = await _context.EvaluationPeriods
            .Where(p => p.SemesterNumber == semesterNumber && !p.IsDeleted)
            .ToListAsync();
        return periods.Select(p => p.Id).ToHashSet();
    }
}

