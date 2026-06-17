using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.BLL.AI.Tools;

public class ParentToolService : IParentToolService
{
    private readonly IParentStudentService _parentStudentService;
    private readonly IStudentEvaluationService _evalService;
    private readonly IPeriodicAssessmentService _periodicService;
    private readonly IExamService _examService;
    private readonly IPeriodAverageService _periodAverageService;

    public ParentToolService(
        IParentStudentService parentStudentService,
        IStudentEvaluationService evalService,
        IPeriodicAssessmentService periodicService,
        IExamService examService,
        IPeriodAverageService periodAverageService)
    {
        _parentStudentService = parentStudentService;
        _evalService = evalService;
        _periodicService = periodicService;
        _examService = examService;
        _periodAverageService = periodAverageService;
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
            Description = "جلب تقييمات الابن (دراسية + تدريبية)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب" },
                    periodId = new { type = "integer", description = "معرف فترة التقييم (اختياري)" },
                    term = new { type = "integer", description = "الفصل الدراسي (اختياري): 1 = الترم الأول, 2 = الترم الثاني, 3 = النهائي (افتراضي = الترم الحالي)" }
                },
                required = new[] { "enrollmentId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();
                var pId = doc.RootElement.TryGetProperty("periodId", out var periodEl) ? periodEl.GetInt32() : 0;

                AcademicTerm? term = context.CurrentTerm;
                if (doc.RootElement.TryGetProperty("term", out var termEl) && termEl.ValueKind == JsonValueKind.Number)
                    term = (AcademicTerm)termEl.GetInt32();

                var academic = await _evalService.GetByEnrollmentAndPeriodAsync(eId, pId, term);
                var training = await _periodicService.GetByEnrollmentAsync(eId, term);
                return JsonSerializer.Serialize(new
                {
                    academicEvaluations = academic.Data,
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
                Description = "جلب الامتحانات القادمة للابن",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        classId = new { type = "integer", description = "معرف الفصل" }
                    },
                    required = new[] { "classId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var cId = doc.RootElement.GetProperty("classId").GetInt32();
                    var result = await _examService.GetUpcomingExamsAsync(cId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        return list.ToDictionary(t => t.Name);
    }
}
