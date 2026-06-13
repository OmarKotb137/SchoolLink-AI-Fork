using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.DTOs.Exam;
using Project.BLL.DTOs.QuestionBank;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Tools;

public class TeacherToolService : ITeacherToolService
{
    private readonly ISubjectService _subjectService;
    private readonly ILessonRepository _lessonRepo;
    private readonly IExamService _examService;
    private readonly IAiExamGeneratorService _aiExamGen;
    private readonly IQuestionBankService _questionBankService;
    private readonly IUnitOfWork _unitOfWork;

    public TeacherToolService(
        ISubjectService subjectService,
        ILessonRepository lessonRepo,
        IExamService examService,
        IAiExamGeneratorService aiExamGen,
        IQuestionBankService questionBankService,
        IUnitOfWork unitOfWork)
    {
        _subjectService = subjectService;
        _lessonRepo = lessonRepo;
        _examService = examService;
        _aiExamGen = aiExamGen;
        _questionBankService = questionBankService;
        _unitOfWork = unitOfWork;
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
                Description = "جلب المواد المتاحة للمدرس (أسماء فقط بدون فصول)",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new object(),
                    required = Array.Empty<string>()
                }),
                ExecuteAsync = async (args) =>
                {
                    var result = await _subjectService.GetSubjectsByTeacherAsync(tId, yId);
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
            Description = "توليد أسئلة امتحان بالذكاء الاصطناعي بناءً على المحتوى الدراسي للوحدة/الدروس وحفظها مباشرة. استخدم get_subjects أولاً لمعرفة subjectId.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    subjectId = new { type = "integer", description = "معرف المادة (من get_subjects) — بديل عن classSubjectTeacherId" },
                    classSubjectTeacherId = new { type = "integer", description = "معرف المادة-الفصل-المدرس (اختياري — لو محططش، الامتحان مش بيتقيد بفصل)" },
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
                required = new[] { "title" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);

                int? cstId = null;
                int? resolvedSubjectId = null;
                if (doc.RootElement.TryGetProperty("classSubjectTeacherId", out var cstEl) && cstEl.ValueKind == JsonValueKind.Number)
                {
                    cstId = cstEl.GetInt32();
                }
                else if (doc.RootElement.TryGetProperty("subjectId", out var subjEl) && subjEl.ValueKind == JsonValueKind.Number)
                {
                    resolvedSubjectId = subjEl.GetInt32();
                }
                else
                {
                    return JsonSerializer.Serialize(new { error = "يجب توفير subjectId أو classSubjectTeacherId" });
                }

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
                    SubjectId = resolvedSubjectId,
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
            Description = "حفظ سؤال واحد أو أكثر في بنك الأسئلة (الموضوع مطلوب). يستخدم لتجميع الأسئلة في بنك مركزي لاستخدامها لاحقاً.",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    subjectId = new { type = "integer", description = "معرف المادة (من get_subjects: الـ subjectId)" },
                    sourceExamId = new { type = "integer", description = "معرف الامتحان المصدر (اختياري)" },
                    questions = new
                    {
                        type = "array",
                        description = "مصفوفة الأسئلة المراد حفظها",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                questionText = new { type = "string", description = "نص السؤال" },
                                questionType = new { type = "integer", description = "نوع السؤال: 1=اختيار من متعدد, 2=صح/خطأ, 3=أكمل الفراغ, 4=مقالي" },
                                correctAnswer = new { type = "string", description = "الإجابة الصحيحة (للأكمل والمقالي)" },
                                options = new
                                {
                                    type = "array",
                                    description = "الخيارات (لـ MCQ وصح/خطأ)",
                                    items = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            text = new { type = "string", description = "نص الخيار" },
                                            isCorrect = new { type = "boolean", description = "هل هذا هو الخيار الصحيح؟" },
                                            displayOrder = new { type = "integer", description = "ترتيب العرض" }
                                        }
                                    }
                                }
                            },
                            required = new[] { "questionText", "questionType" }
                        }
                    }
                },
                required = new[] { "subjectId", "questions" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var subjectId = doc.RootElement.GetProperty("subjectId").GetInt32();
                var sourceExamId = doc.RootElement.TryGetProperty("sourceExamId", out var se) ? se.GetInt32() : (int?)null;

                var questionsArray = doc.RootElement.GetProperty("questions").EnumerateArray();

                var addedCount = 0;
                var duplicateCount = 0;

                foreach (var qEl in questionsArray)
                {
                    var dto = new DTOs.QuestionBank.AddToQuestionBankDto
                    {
                        QuestionText = qEl.GetProperty("questionText").GetString() ?? "",
                        QuestionType = qEl.TryGetProperty("questionType", out var qt) ? qt.GetInt32() : 1,
                        CorrectAnswer = qEl.TryGetProperty("correctAnswer", out var ca) ? ca.GetString() : null,
                        SubjectId = subjectId,
                        SourceExamId = sourceExamId,
                        Options = new()
                    };

                    if (qEl.TryGetProperty("options", out var opts) && opts.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var opt in opts.EnumerateArray())
                        {
                            dto.Options.Add(new DTOs.QuestionBank.AddOptionDto
                            {
                                Text = opt.GetProperty("text").GetString() ?? "",
                                IsCorrect = opt.TryGetProperty("isCorrect", out var ic) && ic.GetBoolean(),
                                DisplayOrder = opt.TryGetProperty("displayOrder", out var od) ? od.GetInt32() : 0
                            });
                        }
                    }

                    var result = await _questionBankService.AddQuestionAsync(dto);
                    if (result.IsSuccess)
                    {
                        if (result.Message?.Contains("موجود مسبقاً") == true)
                            duplicateCount++;
                        else
                            addedCount++;
                    }
                }

                return JsonSerializer.Serialize(new
                {
                    success = true,
                    addedCount,
                    duplicateCount,
                    message = $"تم إضافة {addedCount} سؤال جديد إلى بنك الأسئلة" +
                              (duplicateCount > 0 ? $"، و{duplicateCount} أسئلة موجودة مسبقاً (تم زيادة عدد استخداماتها)" : "")
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        list.Add(new AiTool
        {
            Name = "add_exam_to_question_bank",
            Description = "إضافة جميع أسئلة امتحان موجود إلى بنك الأسئلة (يستخدم بعد توليد امتحان أو من الامتحانات السابقة)",
            Parameters = JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    examUid = new { type = "string", description = "UID الامتحان (من رابط المعاينة)" },
                    subjectId = new { type = "integer", description = "معرف المادة (من get_subjects: الـ subjectId)" }
                },
                required = new[] { "examUid", "subjectId" }
            }),
            ExecuteAsync = async (args) =>
            {
                using var doc = JsonDocument.Parse(args);
                var examUid = doc.RootElement.GetProperty("examUid").GetString() ?? "";
                var subjectId = doc.RootElement.GetProperty("subjectId").GetInt32();

                if (!Guid.TryParse(examUid, out var uid))
                    return JsonSerializer.Serialize(new { error = "UID غير صحيح" });

                var exam = await _examService.GetByUidAsync(uid);
                if (!exam.IsSuccess || exam.Data is null)
                    return JsonSerializer.Serialize(new { error = "الامتحان غير موجود" });

                var result = await _questionBankService.BulkAddFromExamAsync(exam.Data.Id, subjectId);
                return JsonSerializer.Serialize(new
                {
                    success = result.IsSuccess,
                    addedCount = result.Data,
                    message = result.Message ?? "تم إضافة أسئلة الامتحان إلى بنك الأسئلة"
                }, new JsonSerializerOptions { WriteIndented = true });
            }
        });

        return list.ToDictionary(t => t.Name);
    }
}
