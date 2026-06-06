using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Services;

public class StudyScheduleOptimizerService : IStudyScheduleOptimizerService
{
    private readonly ILLMRouter _router;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IExamService _examService;
    private readonly ILogger<StudyScheduleOptimizerService> _logger;

    private const string SystemPrompt =
        "أنت مُحسِّن جداول دراسية. بناءً على المواد والأوقات المتاحة ونقاط الضعف، قدّم جدولاً دراسياً متوازناً باللغة العربية.";

    public StudyScheduleOptimizerService(
        ILLMRouter router,
        IUnitOfWork unitOfWork,
        IExamService examService,
        ILogger<StudyScheduleOptimizerService> logger)
    {
        _router = router;
        _unitOfWork = unitOfWork;
        _examService = examService;
        _logger = logger;
    }

    public async Task<OperationResult<List<object>>> OptimizeScheduleAsync(StudyPlanOptimizationRequest request, CancellationToken ct = default)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<List<object>>.Failure("التسجيل غير موجود", 404);

        var subjects = await _unitOfWork.ClassSubjectTeachers.GetByClassAndYearAsync(enrollment.ClassId, enrollment.AcademicYearId);
        var subjectNames = subjects.Select(s => s.Subject?.Name ?? "غير معروف");

        var weak = request.WeakSubjects.Count > 0
            ? $"المواد الضعيفة: {string.Join(", ", request.WeakSubjects)}"
            : "";

        var examsResult = await _examService.GetExamsByStudentAsync(request.EnrollmentId);
        List<ExamSummaryDto>? upcomingExams = null;
        if (examsResult.IsSuccess && examsResult.Data != null)
        {
            upcomingExams = examsResult.Data
                .Where(e => e.IsPublished && e.StartTime.HasValue && e.StartTime.Value >= DateTime.UtcNow)
                .OrderBy(e => e.StartTime)
                .ToList();
        }

        var examInfo = "";
        if (upcomingExams?.Count > 0)
        {
            var examLines = upcomingExams.Where(e => e.StartTime.HasValue).Select(e =>
                $"- {e.SubjectName}: \"{e.Title}\" في {e.StartTime:yyyy-MM-dd HH:mm}");
            examInfo = $"الامتحانات القادمة:\n{string.Join("\n", examLines)}\n\n";
        }

        var prompt = $"ضع جدولاً دراسياً لمدة {request.AvailableDays} أيام، {request.HoursPerDay} ساعة يومياً.\n" +
                     $"المواد: {string.Join(", ", subjectNames)}\n{weak}\n\n{examInfo}" +
                     "راعِ أولوية المواد التي لها امتحانات قريبة وخصص لها وقتاً أكبر. " +
                     "قسّم الوقت بين المواد بالتساوي مع زيادة طفيفة للمواد الضعيفة.";

        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);

        return OperationResult<List<object>>.Success(new List<object>
        {
            new { schedule = result, days = request.AvailableDays, hoursPerDay = request.HoursPerDay, upcomingExams = upcomingExams }
        });
    }

    public async Task<OperationResult<List<object>>> GetRecommendedScheduleAsync(int enrollmentId, CancellationToken ct = default)
    {
        return await OptimizeScheduleAsync(new StudyPlanOptimizationRequest
        {
            EnrollmentId = enrollmentId,
            AvailableDays = 7,
            HoursPerDay = 3
        }, ct);
    }
}
