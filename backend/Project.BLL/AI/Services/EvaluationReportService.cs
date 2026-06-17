using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Enums;

namespace Project.BLL.AI.Services;

public class EvaluationReportService : IEvaluationReportService
{
    private readonly ILLMRouter _router;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentEvaluationService _evalService;
    private readonly ILogger<EvaluationReportService> _logger;

    private const string SystemPrompt =
        "أنت مولد تقارير تقييم. بناءً على بيانات الطالب والتقييمات، قم بتوليد تقرير أكاديمي مفصل باللغة العربية.";

    public EvaluationReportService(
        ILLMRouter router,
        IUnitOfWork unitOfWork,
        IStudentEvaluationService evalService,
        ILogger<EvaluationReportService> logger)
    {
        _router = router;
        _unitOfWork = unitOfWork;
        _evalService = evalService;
        _logger = logger;
    }

    public async Task<OperationResult<string>> GenerateStudentReportAsync(int studentId, int periodId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<string>.Failure("الطالب غير موجود", 404);

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period == null || period.IsDeleted)
            return OperationResult<string>.Failure("فترة التقييم غير موجودة", 404);

        var termInfo = term.HasValue ? $"\nالفصل الدراسي: {term.Value}" : "";
        var prompt = $"ولّد تقريراً أكاديمياً للطالب (ID: {studentId}) عن فترة التقييم (ID: {periodId}) " +
                     $"اسم الفترة: {period.Name}\nتاريخ البداية: {period.StartDate}\nتاريخ النهاية: {period.EndDate}{termInfo}";

        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<string>.Success(result);
    }

    public async Task<OperationResult<string>> GenerateClassReportAsync(int classId, int periodId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var termInfo = term.HasValue ? $" في الفصل الدراسي {term.Value}" : "";
        var prompt = $"ولّد تقريراً عن أداء الفصل (ID: {classId}) في فترة التقييم (ID: {periodId}){termInfo}";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<string>.Success(result);
    }

    public async Task<OperationResult<string>> GenerateRecommendationsAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var termInfo = term.HasValue ? $" في الفصل الدراسي {term.Value}" : "";
        var prompt = $"قدّم توصيات أكاديمية لتحسين أداء الطالب (ID: {studentId}){termInfo}";
        var result = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);
        return OperationResult<string>.Success(result);
    }
}
