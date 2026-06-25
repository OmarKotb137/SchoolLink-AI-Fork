using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Enums;

namespace Project.BLL.AI.Tools;

public class ParentToolService : IParentToolService
{
    private readonly IParentStudentService _parentStudentService;
    private readonly IStudentEvaluationService _evalService;
    private readonly IPeriodicAssessmentService _periodicService;
    private readonly IExamService _examService;
    private readonly IPeriodAverageService _periodAverageService;
    private readonly IUnitOfWork _unitOfWork;

    public ParentToolService(
        IParentStudentService parentStudentService,
        IStudentEvaluationService evalService,
        IPeriodicAssessmentService periodicService,
        IExamService examService,
        IPeriodAverageService periodAverageService,
        IUnitOfWork unitOfWork)
    {
        _parentStudentService = parentStudentService;
        _evalService = evalService;
        _periodicService = periodicService;
        _examService = examService;
        _periodAverageService = periodAverageService;
        _unitOfWork = unitOfWork;
    }

    public Dictionary<string, AiTool> CreateTools(UserContext context, CancellationToken ct = default)
    {
        var list = new List<AiTool>();

        if (context.ParentId.HasValue)
        {
            var pId = context.ParentId.Value;
            list.Add(new AiTool
            {
                Name = "get_my_children",
                Description = "جلب قائمة أبناء ولي الأمر",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _parentStudentService.GetStudentsByParentAsync(pId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        list.Add(new AiTool
        {
            Name = "get_child_evaluations",
            Description = "جلب تقييمات الابن (دراسية: غياب، سلوك، واجبات، تفاعل + تدريبية: نتائج الامتحانات والواجبات)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب" },
                    periodId = new { type = "integer", description = "معرف فترة التقييم (اختياري — لو مش مذكور يرجع ملخص لكل الفترات)" },
                    term = new { type = "integer", description = "الفصل الدراسي (اختياري): 1 = الترم الأول, 2 = الترم الثاني, 3 = النهائي (افتراضي = الترم الحالي)" }
                },
                required = new[] { "enrollmentId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();

                AcademicTerm? term = context.CurrentTerm;
                if (doc.RootElement.TryGetProperty("term", out var termEl) && termEl.ValueKind == JsonValueKind.Number)
                    term = (AcademicTerm)termEl.GetInt32();

                // Training assessments always works
                var training = await _periodicService.GetByEnrollmentAsync(eId, term);

                // لو periodId مدخول → نجيب تقييمات فترة محددة
                if (doc.RootElement.TryGetProperty("periodId", out var periodEl) && periodEl.ValueKind == JsonValueKind.Number)
                {
                    var pId = periodEl.GetInt32();
                    var academic = await _evalService.GetByEnrollmentAndPeriodAsync(eId, pId, term);
                    return JsonSerializer.Serialize(new
                    {
                        academicEvaluations = academic.Data,
                        trainingAssessments = training.Data
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                // لو periodId مش مدخول → نجيب كل أسابيع الترم ونحسب المتوسطات
                var yearId = context.AcademicYearId;
                if (!yearId.HasValue)
                {
                    return JsonSerializer.Serialize(new
                    {
                        academicEvaluations = Enumerable.Empty<object>(),
                        trainingAssessments = training.Data
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                int semesterNumber = term.HasValue ? (int)term.Value : 1;
                var periods = await _unitOfWork.EvaluationPeriods.GetWeeksByYearAndSemesterAsync(yearId.Value, semesterNumber);
                if (periods is null || periods.Count == 0)
                {
                    return JsonSerializer.Serialize(new
                    {
                        academicEvaluations = Enumerable.Empty<object>(),
                        trainingAssessments = training.Data
                    }, new JsonSerializerOptions { WriteIndented = true });
                }

                // نجيب كل التقييمات لكل الفترات
                var itemsAvgDict = new Dictionary<string, List<decimal>>();
                var weeklyBreakdown = new List<object>();

                foreach (var period in periods)
                {
                    var periodResult = await _evalService.GetByEnrollmentAndPeriodAsync(eId, period.Id, term);
                    var periodEvals = periodResult.Data ?? Enumerable.Empty<Project.BLL.DTOs.StudentEvaluations.StudentEvaluationDto>();

                    decimal weekScore = 0;
                    decimal weekMax = 0;

                    foreach (var eval in periodEvals)
                    {
                        weekScore += eval.Score ?? 0;
                        weekMax += eval.MaxScore;

                        var key = eval.ItemName ?? "أخرى";
                        if (!itemsAvgDict.ContainsKey(key))
                            itemsAvgDict[key] = new List<decimal>();
                    }

                    if (weekMax > 0)
                    {
                        weeklyBreakdown.Add(new
                        {
                            periodName = period.Name,
                            percentage = Math.Round(weekScore / weekMax * 100, 1),
                            score = Math.Round(weekScore, 1),
                            maxScore = Math.Round(weekMax, 1)
                        });
                    }
                }

                // متوسط النسب لكل بند تقييم (بدون أرقام مجمعة)
                foreach (var period in periods)
                {
                    var periodResult = await _evalService.GetByEnrollmentAndPeriodAsync(eId, period.Id, term);
                    var periodEvals = periodResult.Data ?? Enumerable.Empty<Project.BLL.DTOs.StudentEvaluations.StudentEvaluationDto>();

                    foreach (var eval in periodEvals)
                    {
                        var key = eval.ItemName ?? "أخرى";
                        var pct = eval.MaxScore > 0 ? Math.Round((eval.Score ?? 0) / eval.MaxScore * 100, 1) : 0;
                        itemsAvgDict[key].Add(pct);
                    }
                }

                var summaryItems = itemsAvgDict.Select(kv => new
                {
                    name = kv.Key,
                    averagePercentage = kv.Value.Count > 0 ? Math.Round(kv.Value.Average(), 1) : 0
                }).ToList();

                var overallAvgPct = weeklyBreakdown
                    .Select(w => w.GetType().GetProperty("percentage")?.GetValue(w))
                    .Where(v => v is decimal || v is double)
                    .Select(v => Convert.ToDecimal(v))
                    .ToList();

                var overallAvg = overallAvgPct.Count > 0
                    ? Math.Round(overallAvgPct.Average(), 1)
                    : 0m;

                return JsonSerializer.Serialize(new
                {
                    academicEvaluations = new
                    {
                        overallAveragePercentage = overallAvg,
                        totalWeeks = periods.Count,
                        itemsSummary = summaryItems,
                        weeklyBreakdown
                    },
                    trainingAssessments = training.Data
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        list.Add(new AiTool
        {
            Name = "get_child_performance_trend",
            Description = "جلب مؤشرات الأداء عبر الفترات لكشف الانخفاض أو التحسن",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب" },
                    term = new { type = "integer", description = "الفصل الدراسي (اختياري): 1 = الترم الأول, 2 = الترم الثاني, 3 = النهائي (افتراضي = الترم الحالي)" }
                },
                required = new[] { "enrollmentId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();

                AcademicTerm? term = context.CurrentTerm;
                if (doc.RootElement.TryGetProperty("term", out var termEl) && termEl.ValueKind == JsonValueKind.Number)
                    term = (AcademicTerm)termEl.GetInt32();

                var result = await _periodAverageService.GetByEnrollmentAsync(eId, term);
                return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        if (context.AcademicYearId.HasValue)
        {
            var yId = context.AcademicYearId.Value;
            list.Add(new AiTool
            {
                Name = "get_child_upcoming_exams",
                Description = "جلب الامتحانات القادمة للابن باستخدام معرف تسجيل الطالب (enrollmentId)",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب (يظهر في نتائج get_my_children و get_child_evaluations)" }
                    },
                    required = new[] { "enrollmentId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();

                    // Resolve enrollment → classId
                    var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(eId);
                    if (enrollment is null || enrollment.IsDeleted)
                        return JsonSerializer.Serialize(new { error = "الطالب غير موجود أو غير مسجل في أي فصل" });

                    int cId = enrollment.ClassId;
                    var result = await _examService.GetUpcomingExamsAsync(cId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        // ─────────────────────────────────────────
        // TOOL: get_child_subject_performance
        // ─────────────────────────────────────────
        list.Add(new AiTool
        {
            Name = "get_child_subject_performance",
            Description = "جلب أداء الابن الدراسي مفصلاً حسب كل مادة على حدة (وليس إجمالي جميع المواد). كل مادة بدرجاتها ومعدلاتها في فتراتها المختلفة.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب" }
                },
                required = new[] { "enrollmentId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();

                var result = await _periodAverageService.GetByEnrollmentGroupedBySubjectAsync(eId, context.CurrentTerm);
                if (!result.IsSuccess || result.Data is null)
                    return JsonSerializer.Serialize(new { success = false, message = result.Message ?? "لا توجد بيانات" });
                return JsonSerializer.Serialize(new { success = true, data = result.Data }, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        return list.ToDictionary(t => t.Name);
    }
}
