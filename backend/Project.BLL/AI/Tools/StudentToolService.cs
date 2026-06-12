using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Tools;

public class StudentToolService : IStudentToolService
{
    private readonly ILessonRepository _lessonRepo;
    private readonly IStudentEvaluationService _evalService;
    private readonly IPeriodicAssessmentService _periodicService;
    private readonly IExamService _examService;
    private readonly ISubjectService _subjectService;
    private readonly IUnitService _unitService;

    public StudentToolService(
        ILessonRepository lessonRepo,
        IStudentEvaluationService evalService,
        IPeriodicAssessmentService periodicService,
        IExamService examService,
        ISubjectService subjectService,
        IUnitService unitService)
    {
        _lessonRepo = lessonRepo;
        _evalService = evalService;
        _periodicService = periodicService;
        _examService = examService;
        _subjectService = subjectService;
        _unitService = unitService;
    }

    public Dictionary<string, AiTool> CreateTools(UserContext context, CancellationToken ct = default)
    {
        var list = new List<AiTool>();

        // ─────────────────────────────────────────
        // TOOL: search_lessons
        // ─────────────────────────────────────────
        list.Add(new AiTool
        {
            Name = "search_lessons",
            Description = "البحث عن الدروس المتاحة حسب اسم المادة أو كلمة مفتاحية. ترجع قائمة بالدوس مع أسمائها وأرقامها ومعرفاتها.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    keyword = new { type = "string", description = "كلمة البحث — اسم المادة (مثل 'عربي'، 'رياضيات') أو اسم الدرس" }
                },
                required = new[] { "keyword" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var keyword = doc.RootElement.GetProperty("keyword").GetString() ?? "";
                var lessons = await _lessonRepo.SearchAsync(keyword);
                return JsonSerializer.Serialize(lessons, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        // ─────────────────────────────────────────
        // TOOL: get_lesson_content
        // ─────────────────────────────────────────
        list.Add(new AiTool
        {
            Name = "get_lesson_content",
            Description = "جلب محتوى الدرس المطلوب حسب معرف الدرس (lessonId). استخدم search_lessons أولاً لمعرفة lessonId.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    lessonId = new { type = "integer", description = "معرف الدرس الرقمي" }
                },
                required = new[] { "lessonId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var id = doc.RootElement.GetProperty("lessonId").GetInt32();
                var lesson = await _lessonRepo.GetByIdAsync(id);
                return JsonSerializer.Serialize(lesson, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        // ─────────────────────────────────────────
        // TOOL: get_academic_evaluations
        // ─────────────────────────────────────────
        if (context.EnrollmentId.HasValue)
        {
            var eId = context.EnrollmentId.Value;
            list.Add(new AiTool
            {
                Name = "get_academic_evaluations",
                Description = "جلب التقييمات الدراسية للطالب (غياب، سلوك، واجبات، تفاعل)",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        periodId = new { type = "integer", description = "معرف فترة التقييم" }
                    },
                    required = new[] { "periodId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var pId = doc.RootElement.GetProperty("periodId").GetInt32();
                    var result = await _evalService.GetByEnrollmentAndPeriodAsync(eId, pId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });

            list.Add(new AiTool
            {
                Name = "get_training_assessments",
                Description = "جلب نتائج الامتحانات والواجبات للطالب",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _periodicService.GetByEnrollmentAsync(eId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        // ─────────────────────────────────────────
        // TOOL: get_upcoming_exams
        // ─────────────────────────────────────────
        if (context.ClassId.HasValue && context.AcademicYearId.HasValue)
        {
            var cId = context.ClassId.Value;
            var yId = context.AcademicYearId.Value;
            list.Add(new AiTool
            {
                Name = "get_upcoming_exams",
                Description = "جلب الامتحانات القادمة للطالب",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _examService.GetUpcomingExamsAsync(cId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        return list.ToDictionary(t => t.Name);
    }
}
