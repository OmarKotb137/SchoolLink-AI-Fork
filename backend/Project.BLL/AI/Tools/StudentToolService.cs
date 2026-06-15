using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.BLL.Services;

namespace Project.BLL.AI.Tools;

public class StudentToolService : IStudentToolService
{
    private readonly ILessonRepository _lessonRepo;
    private readonly IStudentEvaluationService _evalService;
    private readonly IPeriodicAssessmentService _periodicService;
    private readonly IExamService _examService;
    private readonly ISubjectService _subjectService;
    private readonly IUnitService _unitService;
    private readonly IQuestionEmbeddingService _questionEmbeddingService;

    public StudentToolService(
        ILessonRepository lessonRepo,
        IStudentEvaluationService evalService,
        IPeriodicAssessmentService periodicService,
        IExamService examService,
        ISubjectService subjectService,
        IUnitService unitService,
        IQuestionEmbeddingService questionEmbeddingService)
    {
        _lessonRepo = lessonRepo;
        _evalService = evalService;
        _periodicService = periodicService;
        _examService = examService;
        _subjectService = subjectService;
        _unitService = unitService;
        _questionEmbeddingService = questionEmbeddingService;
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
        // TOOL: get_units — تصفح الوحدات والدروس (أسماء فقط)
        // ─────────────────────────────────────────
        list.Add(new AiTool
        {
            Name = "get_units",
            Description = "جلب أسماء الوحدات والدروس لمادة معينة (بدون المحتوى — فقط للتصفح). استخدم get_unit_content أو get_lesson_content لجلب المحتوى.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    subjectId = new { type = "integer", description = "معرف المادة (اختياري — لو مش معروف، استخدم اسم المادة)" },
                    subjectName = new { type = "string", description = "اسم المادة (اختياري — بديل عن subjectId)" },
                    gradeLevelId = new { type = "integer", description = "معرف الصف الدراسي (اختياري — لتصفية الوحدات حسب الصف)" }
                },
                required = Array.Empty<string>()
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                int? subjectId = null;
                if (doc.RootElement.TryGetProperty("subjectId", out var sid) && sid.ValueKind == JsonValueKind.Number)
                    subjectId = sid.GetInt32();

                if (!subjectId.HasValue && doc.RootElement.TryGetProperty("subjectName", out var sn) && sn.ValueKind == JsonValueKind.String)
                {
                    var subjects = await _subjectService.GetAllSubjectsAsync();
                    var match = (subjects.Data ?? []).FirstOrDefault(s => s.Name.Contains(sn.GetString()!, StringComparison.OrdinalIgnoreCase));
                    subjectId = match?.Id;
                }

                if (!subjectId.HasValue)
                    return JsonSerializer.Serialize(new { error = "لم يتم العثور على المادة. استخدم search_lessons للبحث." });

                int? gradeLevelId = null;
                if (doc.RootElement.TryGetProperty("gradeLevelId", out var gl) && gl.ValueKind == JsonValueKind.Number)
                    gradeLevelId = gl.GetInt32();

                List<UnitDto>? unitsData;
                if (gradeLevelId.HasValue)
                {
                    var result = await _unitService.GetUnitsByGradeLevelAndSubjectAsync(gradeLevelId.Value, subjectId.Value);
                    unitsData = result.Data;
                }
                else
                {
                    var result = await _unitService.GetUnitsWithLessonsBySubjectAsync(subjectId.Value);
                    unitsData = result.Data;
                }

                // أسماء فقط — بدون محتوى
                var light = (unitsData ?? []).Select(u => new
                {
                    id = u.Id,
                    name = u.Name,
                    gradeLevelId = u.GradeLevelId,
                    hasContent = !string.IsNullOrWhiteSpace(u.Content) ||
                                 (u.Lessons is not null && u.Lessons.Any(l => !string.IsNullOrWhiteSpace(l.Content))),
                    lessons = (u.Lessons ?? []).Select(l => new { id = l.Id, title = l.Title }).ToList()
                }).ToList();
                return JsonSerializer.Serialize(light, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        // ─────────────────────────────────────────
        // TOOL: get_unit_content — محتوى وحدة كاملة
        // ─────────────────────────────────────────
        list.Add(new AiTool
        {
            Name = "get_unit_content",
            Description = "جلب المحتوى الكامل لوحدة معينة (محتوى الوحدة + محتوى دروسها). استخدم get_units أولاً لمعرفة subjectId و unitId.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    subjectId = new { type = "integer", description = "معرف المادة (من get_units)" },
                    unitId = new { type = "integer", description = "معرف الوحدة (من get_units)" },
                    gradeLevelId = new { type = "integer", description = "معرف الصف الدراسي (اختياري)" }
                },
                required = new[] { "subjectId", "unitId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var subjectId = doc.RootElement.GetProperty("subjectId").GetInt32();
                var unitId = doc.RootElement.GetProperty("unitId").GetInt32();

                int? gradeLevelId = null;
                if (doc.RootElement.TryGetProperty("gradeLevelId", out var gl) && gl.ValueKind == JsonValueKind.Number)
                    gradeLevelId = gl.GetInt32();

                List<UnitDto>? unitsData;
                if (gradeLevelId.HasValue)
                {
                    var result = await _unitService.GetUnitsByGradeLevelAndSubjectAsync(gradeLevelId.Value, subjectId);
                    unitsData = result.Data;
                }
                else
                {
                    var result = await _unitService.GetUnitsWithLessonsBySubjectAsync(subjectId);
                    unitsData = result.Data;
                }

                var unit = (unitsData ?? []).FirstOrDefault(u => u.Id == unitId);
                if (unit is null)
                    return JsonSerializer.Serialize(new { error = "الوحدة غير موجودة" });
                return JsonSerializer.Serialize(unit, new JsonSerializerOptions { WriteIndented = true });
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

        // ─────────────────────────────────────────
        // TOOL: search_question_bank 🆕
        // ─────────────────────────────────────────
        list.Add(new AiTool
        {
            Name = "search_question_bank",
            Description = "بحث دلالي في بنك الأسئلة عن أسئلة مشابهة. النتائج مُصفاة تلقائياً حسب صف الطالب الدراسي.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string", description = "نص السؤال أو الموضوع المراد البحث عنه" },
                    subjectId = new { type = "integer", description = "معرف المادة (اختياري)" },
                    limit = new { type = "integer", description = "عدد النتائج (اختياري، افتراضي 10)" }
                },
                required = new[] { "query" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var query = doc.RootElement.GetProperty("query").GetString() ?? "";
                var subjectId = doc.RootElement.TryGetProperty("subjectId", out var si) ? si.GetInt32() : (int?)null;
                var limit = doc.RootElement.TryGetProperty("limit", out var li) ? li.GetInt32() : 10;

                var request = new SemanticSearchRequest
                {
                    Query = query,
                    // Auto-filter by student's grade level from context
                    GradeLevelId = context.GradeLevelId,
                    SubjectId = subjectId,
                    Limit = limit,
                    MinScore = 0.4
                };

                var result = await _questionEmbeddingService.SemanticSearchAsync(request);
                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        return list.ToDictionary(t => t.Name);
    }
}
