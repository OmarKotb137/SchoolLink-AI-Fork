using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;

namespace Project.BLL.AI.Tools;

public static class TeacherTools
{
    public static List<AiTool> Create(ISubjectService subjectService, ILessonRepository lessonRepo,
        IExamGeneratorService examGenerator, IExamService examService)
    {
        return new List<AiTool>
        {
            new()
            {
                Name = "get_subjects",
                Description = "جلب المواد المتاحة للمدرس",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        teacherId = new { type = "integer", description = "معرف المدرس" },
                        academicYearId = new { type = "integer", description = "معرف العام الدراسي" }
                    },
                    required = new[] { "teacherId", "academicYearId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var tId = doc.RootElement.GetProperty("teacherId").GetInt32();
                    var yId = doc.RootElement.GetProperty("academicYearId").GetInt32();
                    var result = await subjectService.GetSubjectsByTeacherAsync(tId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            },
            new()
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
                    var lessons = await lessonRepo.SearchAsync(subject);
                    return JsonSerializer.Serialize(lessons, new JsonSerializerOptions { WriteIndented = true });
                }
            },
            new()
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
                    var ok = await lessonRepo.UpdateAsync(id, title, content);
                    return JsonSerializer.Serialize(new { success = ok });
                }
            },
            new()
            {
                Name = "generate_exam_with_ai",
                Description = "توليد امتحان بالذكاء الاصطناعي بناءً على محتوى الدرس أو عناوين محددة",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        lessonContent = new { type = "string", description = "محتوى الدرس لتوليد أسئلة منه" },
                        questionCount = new { type = "integer", description = "عدد الأسئلة" },
                        difficulty = new { type = "string", @enum = new[] { "easy", "medium", "hard" }, description = "مستوى الصعوبة" },
                        style = new { type = "string", @enum = new[] { "multiple_choice", "true_false", "open_ended" }, description = "نوع الأسئلة" }
                    },
                    required = new[] { "lessonContent", "questionCount" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var dto = new DTOs.Exam.CreateExamFromAiDto
                    {
                        Title = "امتحان من AI",
                        DurationMinutes = 60,
                        TotalScore = 100,
                        Category = Domain.Enums.EvaluationCategory.Academic
                    };
                    var result = await examGenerator.GenerateExamAsync(dto);
                    return JsonSerializer.Serialize(new { examId = result.Data?.Id, success = result.IsSuccess });
                }
            },
            new()
            {
                Name = "save_to_question_bank",
                Description = "حفظ الأسئلة في بنك الأسئلة بعد توليدها",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        classSubjectTeacherId = new { type = "integer", description = "معرف المادة" },
                        title = new { type = "string", description = "عنوان الامتحان" },
                        totalScore = new { type = "number", description = "الدرجة الكلية" }
                    },
                    required = new[] { "classSubjectTeacherId", "title" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var dto = new DTOs.Exam.CreateExamFromAiDto
                    {
                        ClassSubjectTeacherId = doc.RootElement.GetProperty("classSubjectTeacherId").GetInt32(),
                        Title = doc.RootElement.GetProperty("title").GetString() ?? "امتحان",
                        TotalScore = doc.RootElement.TryGetProperty("totalScore", out var s) ? s.GetDecimal() : 100,
                        Category = Domain.Enums.EvaluationCategory.Academic
                    };
                    var result = await examService.CreateFromAiAsync(dto);
                    return JsonSerializer.Serialize(new { examId = result.Data?.Id, success = result.IsSuccess });
                }
            }
        };
    }
}
