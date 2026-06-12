using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Tools;

public class TeacherToolService : ITeacherToolService
{
    private readonly ISubjectService _subjectService;
    private readonly ILessonRepository _lessonRepo;
    private readonly IExamService _examService;
    private readonly IAiExamGeneratorService _aiExamGen;

    public TeacherToolService(
        ISubjectService subjectService,
        ILessonRepository lessonRepo,
        IExamService examService,
        IAiExamGeneratorService aiExamGen)
    {
        _subjectService = subjectService;
        _lessonRepo = lessonRepo;
        _examService = examService;
        _aiExamGen = aiExamGen;
    }

    public Dictionary<string, AiTool> CreateTools(UserContext context, CancellationToken ct = default)
    {
        var list = new List<AiTool>();

        if (context.TeacherId.HasValue && context.AcademicYearId.HasValue)
        {
            var tId = context.TeacherId.Value;
            var yId = context.AcademicYearId.Value;

            list.Add(new AiTool
            {
                Name = "get_subjects",
                Description = "جلب المواد المتاحة للمدرس مع classSubjectTeacherId",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _subjectService.GetAssignmentsByTeacherAsync(tId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            });
        }

        list.Add(new AiTool
        {
            Name = "get_lessons",
            Description = "جلب دروس مادة معينة (بحث باسم المادة)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    subject = new { type = "string", description = "اسم المادة للبحث (مثل: رياضيات)" }
                },
                required = new[] { "subject" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var subject = doc.RootElement.GetProperty("subject").GetString() ?? "";
                var lessons = await _lessonRepo.SearchAsync(subject);
                return JsonSerializer.Serialize(lessons, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        list.Add(new AiTool
        {
            Name = "update_lesson",
            Description = "تعديل محتوى درس موجود",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    lessonId = new { type = "integer", description = "معرف الدرس" },
                    title = new { type = "string", description = "العنوان الجديد" },
                    content = new { type = "string", description = "المحتوى الجديد" }
                },
                required = new[] { "lessonId", "title", "content" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var id = doc.RootElement.GetProperty("lessonId").GetInt32();
                var title = doc.RootElement.GetProperty("title").GetString() ?? "";
                var content = doc.RootElement.GetProperty("content").GetString() ?? "";
                var ok = await _lessonRepo.UpdateAsync(id, title, content);
                return JsonSerializer.Serialize(new { success = ok });
            }
        });

        list.Add(new AiTool
        {
            Name = "generate_exam_with_ai",
            Description = "توليد أسئلة امتحان بالذكاء الاصطناعي بناءً على المحتوى الدراسي للوحدة/الدروس وحفظها مباشرة. استخدم get_subjects أولاً لمعرفة classSubjectTeacherId.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    classSubjectTeacherId = new { type = "integer", description = "معرف المادة-الفصل-المدرس (إجباري)" },
                    title = new { type = "string", description = "عنوان الامتحان" },
                    mcqCount = new { type = "integer", description = "عدد أسئلة الاختيار من متعدد (اختياري، افتراضي 5)" },
                    trueFalseCount = new { type = "integer", description = "عدد أسئلة صح/خطأ (اختياري، افتراضي 0)" },
                    fillBlankCount = new { type = "integer", description = "عدد أسئلة أكمل الفراغ (اختياري، افتراضي 0)" },
                    essayCount = new { type = "integer", description = "عدد الأسئلة المقالية (اختياري، افتراضي 0)" },
                    totalScore = new { type = "number", description = "الدرجة الكلية (اختياري، افتراضي 100)" },
                    durationMinutes = new { type = "integer", description = "المدة بالدقائق (اختياري، افتراضي 60)" },
                    unitId = new { type = "integer", description = "معرف الوحدة (اختياري)" },
                    lessonIds = new { type = "array", items = new { type = "integer" }, description = "مصفوفة معرفات الدروس (اختياري)" },
                    topic = new { type = "string", description = "موضوع محدد للامتحان (اختياري)" }
                },
                required = new[] { "classSubjectTeacherId", "title" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);

                var cstId = doc.RootElement.GetProperty("classSubjectTeacherId").GetInt32();
                var title = doc.RootElement.GetProperty("title").GetString() ?? "امتحان من AI";

                var questionCounts = new Dictionary<int, int>
                {
                    [1] = doc.RootElement.TryGetProperty("mcqCount", out var mcq) ? mcq.GetInt32() : 5,
                    [2] = doc.RootElement.TryGetProperty("trueFalseCount", out var tf) ? tf.GetInt32() : 0,
                    [3] = doc.RootElement.TryGetProperty("fillBlankCount", out var fb) ? fb.GetInt32() : 0,
                    [4] = doc.RootElement.TryGetProperty("essayCount", out var essay) ? essay.GetInt32() : 0
                };

                var totalScore = doc.RootElement.TryGetProperty("totalScore", out var ts) ? ts.GetDecimal() : 100m;
                var durationMinutes = doc.RootElement.TryGetProperty("durationMinutes", out var dur) ? dur.GetInt32() : 60;

                int? unitId = null;
                if (doc.RootElement.TryGetProperty("unitId", out var uid) && uid.ValueKind == JsonValueKind.Number)
                    unitId = uid.GetInt32();

                var lessonIds = new List<int>();
                if (doc.RootElement.TryGetProperty("lessonIds", out var lids) && lids.ValueKind == JsonValueKind.Array)
                    lessonIds = lids.EnumerateArray().Select(x => x.GetInt32()).ToList();

                var topic = doc.RootElement.TryGetProperty("topic", out var top) ? top.GetString() : null;

                var request = new AiGenerateExamRequest
                {
                    ClassSubjectTeacherId = cstId,
                    Title = title,
                    DurationMinutes = durationMinutes,
                    TotalScore = totalScore,
                    Category = Domain.Enums.EvaluationCategory.Academic,
                    QuestionCounts = questionCounts,
                    Topic = topic,
                    UnitId = unitId,
                    LessonIds = lessonIds
                };

                var savedExam = await _aiExamGen.GenerateExamAsync(request, ct);
                if (!savedExam.IsSuccess || savedExam.Data is null)
                    return JsonSerializer.Serialize(new { error = savedExam.Message ?? "فشل إنشاء الامتحان" });

                var exam = savedExam.Data;
                return JsonSerializer.Serialize(new
                {
                    examId = exam.Id,
                    title = exam.Title,
                    totalScore = exam.TotalScore,
                    questionsCount = exam.QuestionsCount,
                    viewUrl = $"/exam/{exam.Uid}/html",
                    message = "تم إنشاء الامتحان وحفظه بنجاح. يمكنك معاينته من الرابط أعلاه."
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        list.Add(new AiTool
        {
            Name = "save_to_question_bank",
            Description = "حفظ أسئلة امتحان في بنك الأسئلة (استخدمها بعد توليد الامتحان إذا أردت حفظ نسخة إضافية أو تعديل)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    classSubjectTeacherId = new { type = "integer", description = "معرف المادة-الفصل" },
                    title = new { type = "string", description = "عنوان الامتحان" },
                    totalScore = new { type = "number", description = "الدرجة الكلية" },
                    questionsJson = new { type = "string", description = "مصفوفة الأسئلة بصيغة JSON (كل سؤال يحتوي: questionText, questionType, points, displayOrder, options, correctAnswer)" }
                },
                required = new[] { "classSubjectTeacherId", "title", "questionsJson" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var cstId = doc.RootElement.GetProperty("classSubjectTeacherId").GetInt32();
                var title = doc.RootElement.GetProperty("title").GetString() ?? "امتحان";
                var totalScore = doc.RootElement.TryGetProperty("totalScore", out var ts) ? ts.GetDecimal() : 100;
                var questionsJson = doc.RootElement.GetProperty("questionsJson").GetString() ?? "[]";

                try
                {
                    var questions = JsonSerializer.Deserialize<List<AiQuestionDto>>(questionsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var dto = new CreateExamFromAiDto
                    {
                        ClassSubjectTeacherId = cstId,
                        Title = title,
                        TotalScore = totalScore,
                        Category = Domain.Enums.EvaluationCategory.Academic,
                        DurationMinutes = 60,
                        StandaloneQuestions = questions ?? new()
                    };

                    var result = await _examService.CreateFromAiAsync(dto, ct);
                    return JsonSerializer.Serialize(new
                    {
                        examId = result.Data?.Id,
                        success = result.IsSuccess,
                        message = result.IsSuccess ? "تم حفظ الأسئلة بنجاح" : result.Message
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
                catch (JsonException ex)
                {
                    return JsonSerializer.Serialize(new { error = $"خطأ في تحليل JSON: {ex.Message}" });
                }
            }
        });

        return list.ToDictionary(t => t.Name);
    }
}
