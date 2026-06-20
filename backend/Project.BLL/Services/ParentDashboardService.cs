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
            var enrollment = student.Enrollments.FirstOrDefault(e => e.LeftAt == null && !e.IsDeleted);
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

            // If no absences recorded at all, the student is 100% present by definition.
            // Otherwise, calculate relative to standard school year days (180 days).
            var attendanceRate = absCount == 0 ? 100
                : Math.Round(Math.Max(0, 100 - absCount / 180.0 * 100), 1);

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
            // Get weekly period IDs for the current term (used everywhere below)
            var semesterNumber = term.HasValue ? (int)term.Value : (int?)null;
            var weeklyPeriodList = await _context.EvaluationPeriods
                .Where(p =>
                    (!semesterNumber.HasValue || p.SemesterNumber == semesterNumber) &&
                    p.PeriodType == PeriodType.Weekly &&
                    !p.IsDeleted)
                .OrderBy(p => p.Id)
                .ToListAsync();
            var weeklyPeriodIds = weeklyPeriodList.Select(p => p.Id).ToHashSet();

            // Use GetByEnrollmentGroupedBySubjectAsync as the single source of truth
            // for performance, chart, and subject cards — guaranteed consistency
            var subjResult = await _periodAverageService.GetByEnrollmentGroupedBySubjectAsync(enrollment.Id, term);
            decimal performance = 0;
            string totalPct = "—";
            var weekPerformances = new List<WeeklyPerformanceDto>();
            var subjectPerformances = new List<ChildSubjectDto>();
            var monthlyExams = new List<MonthlyExamResultDto>();
            var finalExams = new List<FinalExamResultDto>();

            if (subjResult.IsSuccess && subjResult.Data != null)
            {
                // ── Build per-period aggregates from per-subject data (for chart) ──
                var periodData = new Dictionary<int, (decimal totalScore, decimal totalMax, string name, List<ChildSubjectDto> subjectScores)>();
                foreach (var group in subjResult.Data)
                {
                    foreach (var p in group.Periods)
                    {
                        if (!weeklyPeriodIds.Contains(p.PeriodId)) continue;
                        var pct = p.AvgScore / 100;
                        var earned = pct * p.MaxScore;
                        if (!periodData.ContainsKey(p.PeriodId))
                        {
                            var period = weeklyPeriodList.FirstOrDefault(wp => wp.Id == p.PeriodId);
                            periodData[p.PeriodId] = (0, 0, period?.Name ?? "", new List<ChildSubjectDto>());
                        }
                        var cur = periodData[p.PeriodId];
                        cur.subjectScores.Add(new ChildSubjectDto
                        {
                            SubjectName = group.SubjectName,
                            Score = p.AvgScore > 0 ? Math.Round(p.AvgScore / 100 * p.MaxScore, 1) : 0,
                            MaxScore = p.MaxScore
                        });
                        periodData[p.PeriodId] = (cur.totalScore + earned, cur.totalMax + p.MaxScore, cur.name, cur.subjectScores);
                    }
                }

                int weekNum = 1;
                var allWeekPcts = new List<decimal>();
                foreach (var kv in periodData.OrderBy(kv => kv.Key))
                {
                    var pct = kv.Value.totalMax > 0
                        ? Math.Round(kv.Value.totalScore / kv.Value.totalMax * 100, 1)
                        : 0m;
                    allWeekPcts.Add(pct);
                    var period = weeklyPeriodList.FirstOrDefault(wp => wp.Id == kv.Key);
                    weekPerformances.Add(new WeeklyPerformanceDto
                    {
                        PeriodName = kv.Value.name != "" ? kv.Value.name : $"الأسبوع {weekNum}",
                        WeekNumber = weekNum++,
                        StartDate = period?.StartDate,
                        EndDate = period?.EndDate,
                        AvgScore = pct,
                        MaxScore = 100,
                        TotalScore = kv.Value.totalScore,
                        TotalMaxScore = kv.Value.totalMax,
                        SubjectPerformances = kv.Value.subjectScores
                    });
                }

                // ── الأداء العام = average of all weekly percentages ──
                if (allWeekPcts.Count > 0)
                {
                    performance = Math.Round(allWeekPcts.Average(), 1);
                    totalPct = $"{performance}%";
                }

                // ── Subject performances (latest week) + Monthly/Final exams ──
                foreach (var group in subjResult.Data)
                {
                    // All periods for this subject in this term (includes weekly, monthly, semester)
                    var termPeriods = group.Periods.ToList();

                    // Latest WEEKLY period → subject performance (current)
                    var weeklyOnly = termPeriods.Where(p => weeklyPeriodIds.Contains(p.PeriodId)).ToList();
                    var latest = weeklyOnly.OrderByDescending(p => p.PeriodId).FirstOrDefault();
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
                                    ? (monthIdx == 1 ? "أكتوبر" : "نوفمبر")
                                    : (monthIdx == 1 ? "مارس" : "إبريل");
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
    /// Get the student's own dashboard data (same richness as parent view).
    /// </summary>
    public async Task<OperationResult<ParentChildDto>> GetStudentDashboardAsync(int studentId, int? termValue = null)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<ParentChildDto>.Failure("الطالب غير موجود");

        var enrollment = student.Enrollments?.FirstOrDefault(e => e.LeftAt == null && !e.IsDeleted);
        if (enrollment == null)
        {
            // Try loading from context
            enrollment = await _context.StudentEnrollments
                .Include(e => e.Class).ThenInclude(c => c!.GradeLevel)
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.LeftAt == null && !e.IsDeleted);
        }
        if (enrollment == null)
            return OperationResult<ParentChildDto>.Failure("لا يوجد تسجيل نشط");

        var className = enrollment.Class?.Name ?? "";
        var gradeName = enrollment.Class?.GradeLevel?.Name ?? "";

        var term = termValue.HasValue
            ? (AcademicTerm)termValue.Value
            : await DetectCurrentTermAsync(enrollment.Id);

        var academicYearId = enrollment.Class?.AcademicYearId;
        bool resultsVisible = true;
        if (academicYearId.HasValue && term.HasValue)
        {
            var existsResult = await _resultVisibilityService.ExistsSettingAsync(academicYearId.Value, term.Value);
            if (existsResult.IsSuccess && existsResult.Data)
            {
                var visResult = await _resultVisibilityService.IsResultsVisibleAsync(academicYearId.Value, term.Value);
                resultsVisible = visResult.IsSuccess && visResult.Data;
            }
        }

        var absences = await _unitOfWork.DailyAbsences
            .FindAsync(a => a.EnrollmentId == enrollment.Id && a.IsAbsent && !a.IsDeleted);
        var absCount = absences.Count;
        var excused = absences.Count(a => !string.IsNullOrWhiteSpace(a.Reason));
        var unexcused = absCount - excused;

        // If no absences recorded at all, the student is 100% present by definition.
        // Otherwise, calculate relative to standard school year days (180 days).
        var attendanceRate = absCount == 0 ? 100
            : Math.Round(Math.Max(0, 100 - absCount / 180.0 * 100), 1);

        var assessments = await _unitOfWork.PeriodicAssessments
            .FindAsync(pa => pa.EnrollmentId == enrollment.Id);
        var last = assessments.OrderByDescending(a => a.AssessmentDate).FirstOrDefault();
        var lastScore = last != null ? $"{last.Score}/{last.MaxScore}" : "—";

        var subjectExamMap = assessments
            .Where(a => a.Term == term)
            .GroupBy(a => a.SubjectId ?? 0)
            .ToDictionary(g => g.Key, g => g.ToLookup(a => a.AssessmentType));

        var semesterNumber = term.HasValue ? (int)term.Value : (int?)null;
        var weeklyPeriodList = await _context.EvaluationPeriods
            .Where(p => (!semesterNumber.HasValue || p.SemesterNumber == semesterNumber) &&
                p.PeriodType == PeriodType.Weekly && !p.IsDeleted)
            .OrderBy(p => p.Id)
            .ToListAsync();
        var weeklyPeriodIds = weeklyPeriodList.Select(p => p.Id).ToHashSet();

        var subjResult = await _periodAverageService.GetByEnrollmentGroupedBySubjectAsync(enrollment.Id, term);
        decimal performance = 0;
        string totalPct = "—";
        var weekPerformances = new List<WeeklyPerformanceDto>();
        var subjectPerformances = new List<ChildSubjectDto>();
        var monthlyExams = new List<MonthlyExamResultDto>();
        var finalExams = new List<FinalExamResultDto>();

        if (subjResult.IsSuccess && subjResult.Data != null)
        {
            var periodData = new Dictionary<int, (decimal totalScore, decimal totalMax, string name, List<ChildSubjectDto> subjectScores)>();
            foreach (var group in subjResult.Data)
            {
                foreach (var p in group.Periods)
                {
                    if (!weeklyPeriodIds.Contains(p.PeriodId)) continue;
                    var pct = p.AvgScore / 100;
                    var earned = pct * p.MaxScore;
                    if (!periodData.ContainsKey(p.PeriodId))
                    {
                        var period = weeklyPeriodList.FirstOrDefault(wp => wp.Id == p.PeriodId);
                        periodData[p.PeriodId] = (0, 0, period?.Name ?? "", new List<ChildSubjectDto>());
                    }
                    var cur = periodData[p.PeriodId];
                    cur.subjectScores.Add(new ChildSubjectDto
                    {
                        SubjectName = group.SubjectName,
                        Score = p.AvgScore > 0 ? Math.Round(p.AvgScore / 100 * p.MaxScore, 1) : 0,
                        MaxScore = p.MaxScore
                    });
                    periodData[p.PeriodId] = (cur.totalScore + earned, cur.totalMax + p.MaxScore, cur.name, cur.subjectScores);
                }
            }

            int weekNum = 1;
            var allWeekPcts = new List<decimal>();
            foreach (var kv in periodData.OrderBy(kv => kv.Key))
            {
                var pct = kv.Value.totalMax > 0
                    ? Math.Round(kv.Value.totalScore / kv.Value.totalMax * 100, 1) : 0m;
                allWeekPcts.Add(pct);
                var period = weeklyPeriodList.FirstOrDefault(wp => wp.Id == kv.Key);
                weekPerformances.Add(new WeeklyPerformanceDto
                {
                    PeriodName = kv.Value.name != "" ? kv.Value.name : $"الأسبوع {weekNum}",
                    WeekNumber = weekNum++,
                    StartDate = period?.StartDate,
                    EndDate = period?.EndDate,
                    AvgScore = pct,
                    MaxScore = 100,
                    TotalScore = kv.Value.totalScore,
                    TotalMaxScore = kv.Value.totalMax,
                    SubjectPerformances = kv.Value.subjectScores
                });
            }

            if (allWeekPcts.Count > 0)
            {
                performance = Math.Round(allWeekPcts.Average(), 1);
                totalPct = $"{performance}%";
            }

            foreach (var group in subjResult.Data)
            {
                var termPeriods = group.Periods.ToList();
                var weeklyOnly = termPeriods.Where(p => weeklyPeriodIds.Contains(p.PeriodId)).ToList();
                var latestWeek = weeklyOnly.OrderByDescending(p => p.PeriodId).FirstOrDefault();
                if (latestWeek != null)
                {
                    subjectPerformances.Add(new ChildSubjectDto
                    {
                        SubjectName = group.SubjectName,
                        Score = latestWeek.AvgScore > 0 ? Math.Round(latestWeek.AvgScore / 100 * latestWeek.MaxScore, 1) : 0,
                        MaxScore = latestWeek.MaxScore
                    });
                }

                var monthlyPeriods = termPeriods
                    .Where(p => p.PeriodType == "2")
                    .OrderBy(p => p.PeriodId)
                    .Take(2)
                    .ToList();
                int monthIdx = 1;
                var subjLookup = subjectExamMap.GetValueOrDefault(group.SubjectId);
                foreach (var mp in monthlyPeriods)
                {
                    var examType = monthIdx == 1 ? PeriodicAssessmentType.MonthlyExam1 : PeriodicAssessmentType.MonthlyExam2;
                    var hasActualExam = subjLookup?.Contains(examType) == true;
                    var periodicAssessment = hasActualExam ? subjLookup![examType].First() : null;
                    var actualMaxScore = hasActualExam ? periodicAssessment!.MaxScore : mp.MaxScore;
                    var actualScore = hasActualExam ? periodicAssessment!.Score
                        : (mp.AvgScore > 0 ? Math.Round(mp.AvgScore / 100 * actualMaxScore, 1) : 0);

                    string monthName;
                    if (periodicAssessment?.AssessmentDate.HasValue == true)
                    {
                        monthName = periodicAssessment.AssessmentDate.Value.Month switch
                        {
                            1 => "يناير", 2 => "فبراير", 3 => "مارس", 4 => "إبريل",
                            5 => "مايو", 6 => "يونيو", 7 => "يوليو", 8 => "أغسطس",
                            9 => "سبتمبر", 10 => "أكتوبر", 11 => "نوفمبر", 12 => "ديسمبر",
                            _ => $"شهر {monthIdx}"
                        };
                    }
                    else
                    {
                        monthName = term == AcademicTerm.FirstSemester
                            ? (monthIdx == 1 ? "أكتوبر" : "نوفمبر")
                            : (monthIdx == 1 ? "مارس" : "إبريل");
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

                var semesterPeriod = termPeriods.FirstOrDefault(p => p.PeriodType == "3");
                if (semesterPeriod != null)
                {
                    var hasFinalExam = subjLookup?.Contains(PeriodicAssessmentType.SemesterExam) == true
                                    || subjLookup?.Contains(PeriodicAssessmentType.FinalAssessment) == true;
                    var semAssessment = hasFinalExam
                        ? (subjLookup![PeriodicAssessmentType.SemesterExam].FirstOrDefault()
                           ?? subjLookup[PeriodicAssessmentType.FinalAssessment].FirstOrDefault()) : null;

                    decimal finalExamScore = 0, finalExamMaxScore = 0;
                    if (hasFinalExam && semAssessment != null)
                    {
                        finalExamScore = semAssessment.Score;
                        finalExamMaxScore = semAssessment.MaxScore;
                    }
                    else
                    {
                        var finalGrade = await _context.FinalGrades
                            .Where(fg => fg.EnrollmentId == enrollment.Id && fg.SubjectId == group.SubjectId && fg.Term == term && !fg.IsDeleted)
                            .OrderByDescending(fg => fg.MaxTotal)
                            .FirstOrDefaultAsync();
                        if (finalGrade != null && finalGrade.FinalExamScore > 0)
                        {
                            finalExamScore = finalGrade.FinalExamScore;
                            var yearWorkMax = monthlyPeriods.FirstOrDefault()?.MaxScore ?? semesterPeriod.MaxScore;
                            var subjAssessments = subjectExamMap.GetValueOrDefault(group.SubjectId);
                            var exam1Max = subjAssessments?.Contains(PeriodicAssessmentType.MonthlyExam1) == true
                                ? subjAssessments![PeriodicAssessmentType.MonthlyExam1].First().MaxScore : 0;
                            var exam2Max = subjAssessments?.Contains(PeriodicAssessmentType.MonthlyExam2) == true
                                ? subjAssessments![PeriodicAssessmentType.MonthlyExam2].First().MaxScore : 0;
                            finalExamMaxScore = finalGrade.MaxTotal - yearWorkMax - exam1Max - exam2Max;
                            if (finalExamMaxScore <= 0) finalExamMaxScore = 30;
                            hasFinalExam = true;
                        }
                    }

                    var fActualMaxScore = hasFinalExam ? finalExamMaxScore : semesterPeriod.MaxScore;
                    var fActualScore = hasFinalExam ? finalExamScore
                        : (semesterPeriod.AvgScore > 0 ? Math.Round(semesterPeriod.AvgScore / 100 * fActualMaxScore, 1) : 0);

                    finalExams.Add(new FinalExamResultDto
                    {
                        SubjectName = group.SubjectName,
                        Title = "امتحان نهاية الترم",
                        Score = fActualScore,
                        MaxScore = fActualMaxScore
                    });
                }
            }
        }

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
                            currentSection = new RecommendationSectionDto { Title = line[2..endIdx].Trim() };
                            recSections.Add(currentSection);
                            var rest = line[(endIdx + 2)..].Trim();
                            if (rest.Length > 0) currentSection.Items.Add(rest);
                            continue;
                        }
                    }
                    if ((line.StartsWith('-') || line.StartsWith('•') || line.StartsWith('*')) && currentSection != null)
                        currentSection.Items.Add(line.TrimStart('-', '•', '*', ' ').Trim());
                }
            }
        }
        catch { /* skip */ }

        if (!resultsVisible) finalExams.Clear();

        var termName = term switch
        {
            AcademicTerm.FirstSemester => "الأول",
            AcademicTerm.SecondSemester => "الثاني",
            AcademicTerm.Final => "النهائي",
            _ => ""
        };

        var dto = new ParentChildDto
        {
            Name = student.FullName,
            Grade = gradeName,
            Class = className,
            Performance = performance,
            Grades = new ChildGradesDto { Last = lastScore, Total = totalPct },
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
        };

        return OperationResult<ParentChildDto>.Success(dto, "تم جلب بيانات dashboard الطالب بنجاح");
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

