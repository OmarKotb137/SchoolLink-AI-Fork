using System.Text.Json;
using Project.BLL.AI.Models;
using Project.BLL.Interfaces;

namespace Project.BLL.AI.Tools;

public static class ParentTools
{
    public static List<AiTool> Create(IParentStudentService parentStudentService,
        IStudentEvaluationService evalService, IPeriodicAssessmentService periodicService,
        IExamService examService, IPeriodAverageService periodAverageService)
    {
        return new List<AiTool>
        {
            new()
            {
                Name = "get_my_children",
                Description = "جلب قائمة أبناء ولي الأمر",
                Parameters = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    properties = new
                    {
                        parentId = new { type = "integer", description = "معرف ولي الأمر" }
                    },
                    required = new[] { "parentId" }
                }),
                ExecuteAsync = async (args) =>
                {
                    using var doc = JsonDocument.Parse(args);
                    var pId = doc.RootElement.GetProperty("parentId").GetInt32();
                    var result = await parentStudentService.GetStudentsByParentAsync(pId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            },
            new()
            {
                Name = "get_child_evaluations",
                Description = "جلب تقييمات الابن (دراسية + تدريبية)",
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
                    var academic = await evalService.GetByEnrollmentAndPeriodAsync(eId, 0);
                    var training = await periodicService.GetByEnrollmentAsync(eId);
                    return JsonSerializer.Serialize(new
                    {
                        academicEvaluations = academic.Data,
                        trainingAssessments = training.Data
                    }, new JsonSerializerOptions { WriteIndented = true });
                }
            },
            new()
            {
                Name = "get_child_upcoming_exams",
                Description = "جلب الامتحانات القادمة للابن",
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
            },
            new()
            {
                Name = "get_child_performance_trend",
                Description = "جلب مؤشرات الأداء عبر الفترات لكشف الانخفاض أو التحسن",
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
                    var result = await periodAverageService.GetByEnrollmentAsync(eId);
                    return JsonSerializer.Serialize(result.Data, new JsonSerializerOptions { WriteIndented = true });
                }
            }
        };
    }
}
