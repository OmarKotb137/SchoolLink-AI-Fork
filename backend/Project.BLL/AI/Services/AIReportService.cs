using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.DTOs.Reports;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.AI.Services;

public class AIReportService : IAIReportService
{
    private readonly ILLMRouter _router;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentEvaluationService _evalService;
    private readonly ILogger<AIReportService> _logger;

    private const string SystemPrompt =
        "أنت مولد تقارير تقييم. بناءً على بيانات الطالب والتقييمات، قم بتوليد تقرير أكاديمي مفصل باللغة العربية.";

    public AIReportService(
        ILLMRouter router,
        IUnitOfWork unitOfWork,
        IStudentEvaluationService evalService,
        ILogger<AIReportService> logger)
    {
        _router = router;
        _unitOfWork = unitOfWork;
        _evalService = evalService;
        _logger = logger;
    }

    public async Task<OperationResult<AIReport>> GenerateStudentReportAsync(int studentId, int periodId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<AIReport>.Failure("الطالب غير موجود", 404);

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period == null || period.IsDeleted)
            return OperationResult<AIReport>.Failure("فترة التقييم غير موجودة", 404);

        var termInfo = term.HasValue ? $"\nالفصل الدراسي: {term.Value}" : "";
        var prompt = $"ولّد تقريراً أكاديمياً للطالب {student.FullName} عن فترة التقييم {period.Name}" +
                     $"\nتاريخ البداية: {period.StartDate}\nتاريخ النهاية: {period.EndDate}{termInfo}" +
                     $"\nملاحظة: السنة الحالية هي {DateTime.UtcNow.Year}، استخدمها في كتابة التاريخ أسفل التقرير.";

        var content = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);

        var report = new AIReport
        {
            StudentId = studentId,
            PeriodId = periodId,
            Term = term,
            ReportType = "Student",
            Content = content,
            IsPublished = false
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<AIReport>.Success(report, "تم إنشاء التقرير بنجاح");
    }

    public async Task<OperationResult<AIReport>> GenerateClassReportAsync(int classId, int periodId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        var periodName = period?.Name ?? "الفترة الحالية";
        var termInfo = term.HasValue ? $" في الفصل الدراسي {term.Value}" : "";
        var prompt = $"ولّد تقريراً عن أداء الفصل في فترة التقييم {periodName}{termInfo}";
        var content = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);

        var report = new AIReport
        {
            ClassId = classId,
            PeriodId = periodId,
            Term = term,
            ReportType = "Class",
            Content = content,
            IsPublished = false
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<AIReport>.Success(report, "تم إنشاء تقرير الفصل بنجاح");
    }

    public async Task<OperationResult<AIReport>> GenerateRecommendationsAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        var studentName = student?.FullName ?? "الطالب";
        var termInfo = term.HasValue ? $" في الفصل الدراسي {term.Value}" : "";
        var prompt = $"قدّم توصيات أكاديمية لتحسين أداء الطالب {studentName}{termInfo}";
        var content = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);

        var report = new AIReport
        {
            StudentId = studentId,
            Term = term,
            ReportType = "Recommendations",
            Content = content,
            IsPublished = false
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<AIReport>.Success(report, "تم إنشاء التوصيات بنجاح");
    }

    public async Task<OperationResult<IEnumerable<AIReport>>> GetStudentReportsAsync(int studentId, int? periodId = null)
    {
        var reports = await _unitOfWork.AIReports
            .FindAsync(r => r.StudentId == studentId && !r.IsDeleted);

        if (periodId.HasValue)
            reports = reports.Where(r => r.PeriodId == periodId).ToList();

        return OperationResult<IEnumerable<AIReport>>.Success(
            reports.OrderByDescending(r => r.CreatedAt));
    }

    public async Task<OperationResult<AIReport>> GetReportByIdAsync(int reportId, int userId, UserRole role)
    {
        var report = await _unitOfWork.AIReports.GetByIdAsync(reportId);
        if (report == null || report.IsDeleted)
            return OperationResult<AIReport>.Failure("التقرير غير موجود", 404);

        if (!role.IsAdminLike())
        {
            if (role == UserRole.Teacher && !report.ClassId.HasValue)
                return OperationResult<AIReport>.Failure("لا يمكنك الوصول إلى هذا التقرير");

            if (role == UserRole.Parent)
            {
                var parentLinks = await _unitOfWork.ParentStudents
                    .FindAsync(ps => ps.ParentId == userId && ps.StudentId == report.StudentId);
                if (!parentLinks.Any())
                    return OperationResult<AIReport>.Failure("لا يمكنك الوصول إلى هذا التقرير");
            }
            else if (role == UserRole.Student && report.StudentId != userId)
            {
                return OperationResult<AIReport>.Failure("لا يمكنك الوصول إلى هذا التقرير");
            }
        }

        return OperationResult<AIReport>.Success(report);
    }

    public async Task<OperationResult<StudentReportDto>> GetStructuredStudentReportAsync(
        int studentId, int periodId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<StudentReportDto>.Failure("الطالب غير موجود", 404);

        // If periodId is 0, resolve to the latest period for this student
        if (periodId == 0)
        {
            periodId = await ResolveLatestPeriodAsync(studentId);
            if (periodId == 0)
                return OperationResult<StudentReportDto>.Failure("لا توجد فترة تقييم متاحة للطالب", 404);
        }

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period == null || period.IsDeleted)
            return OperationResult<StudentReportDto>.Failure("فترة التقييم غير موجودة", 404);

        var enrollment = (await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.StudentId == studentId && e.LeftAt == null))
            .FirstOrDefault();

        // Overall score from FinalGrade (fallback to evaluation-based calculation)
        double overallScore = 0;
        double overallMax = 100;
        decimal periodAvg = 0;

        if (enrollment != null && term.HasValue)
        {
            var finalGrade = await _unitOfWork.FinalGrades
                .GetByEnrollmentIdAsync(enrollment.Id, term, subjectId: null, ct);
            if (finalGrade != null)
            {
                overallScore = (double)(finalGrade.Total > 0 ? finalGrade.Total : finalGrade.PeriodAvgScore);
                overallMax = (double)(finalGrade.MaxTotal > 0 ? finalGrade.MaxTotal : 100);
                periodAvg = finalGrade.PeriodAvgScore;
            }

            // Convert periodAvg raw score to percentage
            if (periodAvg > 0)
            {
                try
                {
                    var schoolClass = await _unitOfWork.Classes.GetByIdAsync(enrollment.ClassId);
                    if (schoolClass != null)
                    {
                        var templates = await _unitOfWork.EvaluationTemplates
                            .GetByGradeLevelAndYearAsync(schoolClass.GradeLevelId, schoolClass.AcademicYearId, term, ct);
                        var template = templates.FirstOrDefault(t => t.Term == null || t.Term == term);
                        if (template != null)
                        {
                            var templateWithItems = await _unitOfWork.EvaluationTemplates
                                .GetWithItemsAsync(template.Id, ct);
                            var yearWorkMax = templateWithItems?.Items?
                                .Where(i => !i.IsDeleted)
                                .Sum(i => i.MaxScore * i.Weight) ?? 0;
                            if (yearWorkMax > 0)
                            {
                                periodAvg = Math.Round(periodAvg / yearWorkMax * 100, 1);
                            }
                        }
                    }
                }
                catch { /* keep raw periodAvg if yearWorkMax calc fails */ }
            }
        }

        // Subject-level grades via evaluations
        var subjectGrades = new List<SubjectGradeDto>();

        if (enrollment != null)
        {
            var evaluations = await _unitOfWork.StudentEvaluations
                .GetByEnrollmentAndPeriodAsync(enrollment.Id, periodId, ct);

            if (evaluations.Count > 0)
            {
                var itemIds = evaluations.Select(e => e.EvaluationItemId).Distinct().ToList();
                var items = (await _unitOfWork.EvaluationItems.FindAsync(i => itemIds.Contains(i.Id))).ToList();
                var templateIds = items.Select(i => i.TemplateId).Distinct().ToList();
                var templates = (await _unitOfWork.EvaluationTemplates.FindAsync(t => templateIds.Contains(t.Id))).ToList();
                var subjectIds = templates.Select(t => t.SubjectId).Distinct().ToList();
                var subjects = (await _unitOfWork.Subjects.FindAsync(s => subjectIds.Contains(s.Id))).ToList();

                var subjectDict = subjects.ToDictionary(s => s.Id, s => s.Name);
                var templateDict = templates.ToDictionary(t => t.Id, t => t.SubjectId);
                var itemDict = items.ToDictionary(i => i.Id, i => new { i.TemplateId, i.MaxScore, i.Name });

                var subjectGroups = new Dictionary<int, (decimal Score, decimal Max, string Name)>();
                foreach (var eval in evaluations)
                {
                    if (!itemDict.TryGetValue(eval.EvaluationItemId, out var itemInfo)) continue;
                    if (!templateDict.TryGetValue(itemInfo.TemplateId, out var subjId)) continue;
                    if (!subjectDict.TryGetValue(subjId, out var subjName)) continue;

                    var score = eval.Score ?? 0;
                    if (!subjectGroups.ContainsKey(subjId))
                        subjectGroups[subjId] = (0, 0, subjName);
                    var cur = subjectGroups[subjId];
                    subjectGroups[subjId] = (cur.Score + score, cur.Max + itemInfo.MaxScore, subjName);
                }

                subjectGrades = subjectGroups.Select(sg => new SubjectGradeDto
                {
                    SubjectName = sg.Value.Name,
                    Score = sg.Value.Score,
                    MaxScore = sg.Value.Max
                }).ToList();
            }
        }

        // If overallScore is still 0 but we have subject grades, calculate from them
        if (overallScore == 0 && subjectGrades.Count > 0)
        {
            overallScore = (double)subjectGrades.Sum(sg => sg.Score);
            overallMax = (double)subjectGrades.Sum(sg => sg.MaxScore);
        }

        // If periodAvg is still 0 but we have subject grades, calculate average percentage
        if (periodAvg == 0 && subjectGrades.Count > 0)
        {
            var totalPct = subjectGrades
                .Where(sg => sg.MaxScore > 0)
                .Sum(sg => (double)(sg.Score / sg.MaxScore * 100));
            periodAvg = Math.Round((decimal)(totalPct / subjectGrades.Count(sg => sg.MaxScore > 0)), 1);
        }

        // Trend detection
        string overallTrend = "stable";
        double overallChange = 0;

        var previousReports = await _unitOfWork.AIReports
            .FindAsync(r => r.StudentId == studentId && r.PeriodId == periodId && r.ReportType == "Student" && !r.IsDeleted);
        var previousReport = previousReports.OrderByDescending(r => r.CreatedAt).Skip(1).FirstOrDefault();

        if (previousReport?.Summary != null)
        {
            try
            {
                var prevData = System.Text.Json.JsonSerializer.Deserialize<StudentReportDto>(previousReport.Summary);
                if (prevData != null)
                {
                    overallChange = overallScore - prevData.OverallScore;
                    overallTrend = overallChange switch
                    {
                        > 2 => "up",
                        < -2 => "down",
                        _ => "stable"
                    };
                }
            }
            catch { /* skip */ }
        }

        // Metrics
        var metrics = new List<MetricDto>
        {
            new() { Label = "المعدل العام", Value = overallScore, Max = overallMax },
            new() { Label = "متوسط التقييمات", Value = (double)periodAvg, Max = 100 }
        };

        if (enrollment != null)
        {
            try
            {
                var absences = await _unitOfWork.DailyAbsences
                    .FindAsync(a => a.EnrollmentId == enrollment.Id && !a.IsDeleted);
                var attendanceRate = Math.Max(0, 100 - absences.Count() / 90.0 * 100);
                metrics.Add(new MetricDto { Label = "نسبة الحضور", Value = Math.Round(attendanceRate, 1), Max = 100 });
            }
            catch { /* skip */ }
        }

        foreach (var sg in subjectGrades)
        {
            metrics.Add(new MetricDto
            {
                Label = sg.SubjectName,
                Value = (double)sg.Score,
                Max = (double)sg.MaxScore
            });
        }

        // Generate AI-enhanced report text with real scores
        var currentYear = DateTime.UtcNow.Year;
        var scoresSummary = string.Join("\n", subjectGrades.Select(sg => $"- {sg.SubjectName}: {sg.Score:F1}/{sg.MaxScore:F1}"));
        var aiPrompt = $"ولّد تقريراً أكاديمياً باللغة العربية للطالب {student.FullName} " +
                       $"عن فترة التقييم: {period.Name}. " +
                       $"الدرجة الكلية: {overallScore:F1}/{overallMax:F1}. " +
                       $"التغيير عن التقرير السابق: {overallChange:+#;-#;0}%. " +
                       $"المواد:\n{scoresSummary}\n\n" +
                       $"اكتب تحليلاً مفصلاً للأداء مع نقاط القوة والضعف." +
                       (term.HasValue ? $"\nالفصل الدراسي: {term.Value}" : "") +
                       $"\nملاحظة: السنة الحالية هي {currentYear}، استخدمها في كتابة التاريخ أسفل التقرير.";

        var aiContent = await _router.GenerateAsync(
            "أنت محلل أكاديمي متخصص في تحليل أداء الطلاب وكتابة تقارير باللغة العربية.",
            aiPrompt, ct: ct);

        // Build DTO
        var reportDto = new StudentReportDto
        {
            StudentId = studentId,
            StudentName = student.FullName,
            PeriodId = periodId,
            PeriodName = period.Name,
            Term = term,
            OverallScore = overallScore,
            OverallMax = overallMax,
            OverallTrend = overallTrend,
            OverallChange = overallChange,
            SubjectGrades = subjectGrades,
            Metrics = metrics,
            ReportText = aiContent
        };

        // Save to AIReports table for history
        var summaryJson = System.Text.Json.JsonSerializer.Serialize(reportDto);
        var report = new AIReport
        {
            StudentId = studentId,
            PeriodId = periodId,
            Term = term,
            ReportType = "Student",
            Content = aiContent,
            Summary = summaryJson,
            IsPublished = true
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<StudentReportDto>.Success(reportDto, "تم إنشاء التقرير بنجاح");
    }

    public async Task<OperationResult<RecommendationsDto>> GetStructuredRecommendationsAsync(
        int studentId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<RecommendationsDto>.Failure("الطالب غير موجود", 404);

        var termInfo = term.HasValue ? $" في الفصل الدراسي {term.Value}" : "";
        var enrollment = (await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.StudentId == studentId && e.LeftAt == null))
            .FirstOrDefault();

        var weakAreas = new List<string>();
        if (enrollment != null)
        {
            var evaluations = await _unitOfWork.StudentEvaluations
                .GetByEnrollmentIdAsync(enrollment.Id, ct);

            if (evaluations.Count > 0)
            {
                var itemIds = evaluations.Select(e => e.EvaluationItemId).Distinct().ToList();
                var items = (await _unitOfWork.EvaluationItems.FindAsync(i => itemIds.Contains(i.Id))).ToList();
                var templateIds = items.Select(i => i.TemplateId).Distinct().ToList();
                var templates = (await _unitOfWork.EvaluationTemplates.FindAsync(t => templateIds.Contains(t.Id))).ToList();
                var subjectIds = templates.Select(t => t.SubjectId).Distinct().ToList();
                var subjects = (await _unitOfWork.Subjects.FindAsync(s => subjectIds.Contains(s.Id))).ToList();

                var subjectDict = subjects.ToDictionary(s => s.Id, s => s.Name);
                var templateDict = templates.ToDictionary(t => t.Id, t => t.SubjectId);
                var itemDict = items.ToDictionary(i => i.Id, i => new { i.TemplateId, i.MaxScore, i.Name });

                var subjectScores = new Dictionary<int, (decimal Score, decimal Max, string Name)>();
                foreach (var eval in evaluations)
                {
                    if (!itemDict.TryGetValue(eval.EvaluationItemId, out var itemInfo)) continue;
                    if (!templateDict.TryGetValue(itemInfo.TemplateId, out var subjId)) continue;
                    if (!subjectDict.TryGetValue(subjId, out var subjName)) continue;

                    var score = eval.Score ?? 0;
                    if (!subjectScores.ContainsKey(subjId))
                        subjectScores[subjId] = (0, 0, subjName);
                    var cur = subjectScores[subjId];
                    subjectScores[subjId] = (cur.Score + score, cur.Max + itemInfo.MaxScore, subjName);
                }

                weakAreas = subjectScores
                    .Where(s => s.Value.Max > 0 && (double)(s.Value.Score / s.Value.Max) < 0.6)
                    .Select(s => s.Value.Name)
                    .ToList();
            }
        }

        var currentYear = DateTime.UtcNow.Year;
        var scoresHint = weakAreas.Count > 0
            ? $" الطالب يحتاج تحسيناً في المواد التالية: {string.Join("، ", weakAreas)}."
            : "";

        var prompt = $"قدّم توصيات أكاديمية مخصصة لتحسين أداء الطالب {student.FullName}{termInfo}.{scoresHint}\n\n" +
                     "قسّم التوصيات إلى أقسام واضحة. كل قسم يبدأ بعنوان بين علامتي ** ** على سطر منفصل.\n" +
                     "تحت كل عنوان، اكتب التوصيات كقائمة نقطية تبدأ كل منها بـ '- '.\n" +
                     "مثال:\n" +
                     "**تحسين المواد الدراسية**\n" +
                     "- توصية 1\n" +
                     "- توصية 2\n\n" +
                     "**تنمية المهارات**\n" +
                     "- توصية 3\n" +
                     "- توصية 4\n\n" +
                     "يجب أن تكون التوصيات محددة وقابلة للتطبيق." +
                     $"\nملاحظة: السنة الحالية هي {currentYear}.";

        var content = await _router.GenerateAsync(
            "أنت مستشار أكاديمي خبير في تقديم توصيات تعليمية مخصصة باللغة العربية.",
            prompt, ct: ct);

        // Parse sections: **Title** blocks followed by - items
        var sections = new List<RecommendationSection>();
        var lines = content.Split('\n', StringSplitOptions.None);

        RecommendationSection? currentSection = null;
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0) continue;

            // Section header: ** something **
            if (line.StartsWith("**") && line.EndsWith("**"))
            {
                currentSection = new RecommendationSection
                {
                    Title = line.Trim('*', ' ').Trim()
                };
                sections.Add(currentSection);
                continue;
            }

            // Also check for bold markdown **text** where text may have more content after **
            if (line.StartsWith("**") && line.Contains("**", StringComparison.Ordinal) && line.LastIndexOf("**") > 2)
            {
                var endIdx = line.LastIndexOf("**");
                var title = line[2..endIdx].Trim();
                if (title.Length > 0)
                {
                    currentSection = new RecommendationSection { Title = title };
                    sections.Add(currentSection);
                    // remaining text after the ** could be content
                    var rest = line[(endIdx + 2)..].Trim();
                    if (rest.Length > 0 && !rest.StartsWith('-') && !rest.StartsWith('•'))
                    {
                        currentSection.Items.Add(rest);
                    }
                    continue;
                }
            }

            // Bullet item
            if (line.StartsWith('-') || line.StartsWith('•') || line.StartsWith('*'))
            {
                var item = line.TrimStart('-', '•', '*', ' ').Trim();
                if (item.Length > 0)
                {
                    if (currentSection != null)
                        currentSection.Items.Add(item);
                    else
                    {
                        // Items before any section header → create a default section
                        currentSection = new RecommendationSection { Title = "توصيات عامة" };
                        sections.Add(currentSection);
                        currentSection.Items.Add(item);
                    }
                }
                continue;
            }

            // Plain text line → append to current section's last item or skip
            if (currentSection != null && currentSection.Items.Count > 0)
            {
                currentSection.Items[^1] += " " + line;
            }
        }

        // Fallback: flat list from old parsing
        var recItems = content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => l.TrimStart().StartsWith('-') || l.TrimStart().StartsWith('•') || l.TrimStart().StartsWith('*'))
            .Select(l => l.TrimStart('-', '•', '*', ' ').Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (recItems.Count == 0)
            recItems.Add(content.Trim());

        var report = new AIReport
        {
            StudentId = studentId,
            Term = term,
            ReportType = "Recommendations",
            Content = content,
            IsPublished = true
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        var dto = new RecommendationsDto
        {
            StudentId = studentId,
            RecommendationsText = content,
            RecommendationItems = recItems,
            Sections = sections
        };

        return OperationResult<RecommendationsDto>.Success(dto, "تم إنشاء التوصيات بنجاح");
    }

    public async Task<OperationResult<object>> DeleteReportAsync(int reportId, int userId, UserRole role)
    {
        var report = await _unitOfWork.AIReports.GetByIdAsync(reportId);
        if (report == null || report.IsDeleted)
            return OperationResult<object>.Failure("التقرير غير موجود", 404);

        // Role-based access
        if (!role.IsAdminLike())
        {
            if (role == UserRole.Teacher && !report.ClassId.HasValue)
                return OperationResult<object>.Failure("لا يمكنك حذف هذا التقرير");

            if (role == UserRole.Parent)
            {
                var parentLinks = await _unitOfWork.ParentStudents
                    .FindAsync(ps => ps.ParentId == userId && ps.StudentId == report.StudentId);
                if (!parentLinks.Any())
                    return OperationResult<object>.Failure("لا يمكنك حذف هذا التقرير");
            }
            else if (role == UserRole.Student && report.StudentId != userId)
            {
                return OperationResult<object>.Failure("لا يمكنك حذف هذا التقرير");
            }
        }

        _unitOfWork.AIReports.SoftDelete(report);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<object>.Success(new { }, "تم حذف التقرير بنجاح");
    }

    /// <summary>
    /// Finds the latest evaluation period ID that has data for the given student.
    /// </summary>
    private async Task<int> ResolveLatestPeriodAsync(int studentId)
    {
        var enrollment = (await _unitOfWork.StudentEnrollments
            .FindAsync(e => e.StudentId == studentId && e.LeftAt == null))
            .FirstOrDefault();
        if (enrollment == null) return 0;

        // Get all periods with evaluations for this student
        var evaluations = await _unitOfWork.StudentEvaluations
            .GetByEnrollmentIdAsync(enrollment.Id, default);
        var periodIds = evaluations.Select(e => e.PeriodId).Distinct().ToList();
        if (periodIds.Count == 0) return 0;

        // Return the most recent period by ID
        return periodIds.Max();
    }
}
