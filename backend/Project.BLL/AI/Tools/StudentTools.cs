using System.Text.Json;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;

namespace Project.BLL.AI.Tools;

public static class StudentTools
{
    public static List<AiTool> Create(ILessonRepository lessonRepo, IStudentEvaluationService evalService,
        IPeriodicAssessmentService periodicService, IExamService examService)
    {
        return new List<AiTool>
        {
            new()
            {
                Name = "get_lesson_content",
                Description = "جلب محتوى الدرس المطلوب حسب معرف الدرس",
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
                    var lesson = await lessonRepo.GetByIdAsync(id);
                    return JsonSerializer.Serialize(lesson, new JsonSerializerOptions { WriteIndented = true });
                }
            },
            new()
            {
                Name = "get_academic_evaluations",
                Description = "جلب التقييمات الدراسية للطالب (غياب، سلوك، واجبات، تفاعل)",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        enrollmentId = new { type = "integer", description = "معرف تسجيل الطالب" },
                        periodId = new { type = "integer", description = "معرف فترة التقييم" }
                    },
                    required = new[] { "enrollmentId", "periodId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var eId = doc.RootElement.GetProperty("enrollmentId").GetInt32();
                    var pId = doc.RootElement.GetProperty("periodId").GetInt32();
                    var result = await evalService.GetByEnrollmentAndPeriodAsync(eId, pId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            },
            new()
            {
                Name = "get_training_assessments",
                Description = "جلب نتائج الامتحانات والواجبات للطالب",
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
                    var result = await periodicService.GetByEnrollmentAsync(eId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            },
            new()
            {
                Name = "get_upcoming_exams",
                Description = "جلب الامتحانات القادمة للطالب",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        classId = new { type = "integer", description = "معرف الفصل" },
                        academicYearId = new { type = "integer", description = "معرف العام الدراسي" }
                    },
                    required = new[] { "classId", "academicYearId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var cId = doc.RootElement.GetProperty("classId").GetInt32();
                    var yId = doc.RootElement.GetProperty("academicYearId").GetInt32();
                    var result = await examService.GetUpcomingExamsAsync(cId, yId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            }
        };
    }
}
