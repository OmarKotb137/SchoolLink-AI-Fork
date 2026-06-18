using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.AI.Services;

public class AIReportService : IAIReportService
{
    private readonly ILLMRouter _router;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentEvaluationService _evalService;
    private readonly ILogger<AIReportService> _logger;

    private const string SystemPrompt =
        "أنت مولد تقارير تقييم. بناءً على بيانات الطالب والتقييمات، قم بتوليد تقرير أكاديمي مفصل باللغة العربية.";

    public AIReportService(
        ILLMRouter router,
        IUnitOfWork unitOfWork,
        IStudentEvaluationService evalService,
        ILogger<AIReportService> logger)
    {
        _router = router;
        _unitOfWork = unitOfWork;
        _evalService = evalService;
        _logger = logger;
    }

    public async Task<OperationResult<AIReport>> GenerateStudentReportAsync(int studentId, int periodId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<AIReport>.Failure("الطالب غير موجود", 404);

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period == null || period.IsDeleted)
            return OperationResult<AIReport>.Failure("فترة التقييم غير موجودة", 404);

        var termInfo = term.HasValue ? $"\nالفصل الدراسي: {term.Value}" : "";
        var prompt = $"ولّد تقريراً أكاديمياً للطالب (ID: {studentId}) عن فترة التقييم (ID: {periodId}) " +
                     $"اسم الفترة: {period.Name}\nتاريخ البداية: {period.StartDate}\nتاريخ النهاية: {period.EndDate}{termInfo}";

        var content = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);

        var report = new AIReport
        {
            StudentId = studentId,
            PeriodId = periodId,
            Term = term,
            ReportType = "Student",
            Content = content,
            IsPublished = false
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<AIReport>.Success(report, "تم إنشاء التقرير بنجاح");
    }

    public async Task<OperationResult<AIReport>> GenerateClassReportAsync(int classId, int periodId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var termInfo = term.HasValue ? $" في الفصل الدراسي {term.Value}" : "";
        var prompt = $"ولّد تقريراً عن أداء الفصل (ID: {classId}) في فترة التقييم (ID: {periodId}){termInfo}";
        var content = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);

        var report = new AIReport
        {
            ClassId = classId,
            PeriodId = periodId,
            Term = term,
            ReportType = "Class",
            Content = content,
            IsPublished = false
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<AIReport>.Success(report, "تم إنشاء تقرير الفصل بنجاح");
    }

    public async Task<OperationResult<AIReport>> GenerateRecommendationsAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var termInfo = term.HasValue ? $" في الفصل الدراسي {term.Value}" : "";
        var prompt = $"قدّم توصيات أكاديمية لتحسين أداء الطالب (ID: {studentId}){termInfo}";
        var content = await _router.GenerateAsync(SystemPrompt, prompt, ct: ct);

        var report = new AIReport
        {
            StudentId = studentId,
            Term = term,
            ReportType = "Recommendations",
            Content = content,
            IsPublished = false
        };

        await _unitOfWork.AIReports.AddAsync(report);
        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<AIReport>.Success(report, "تم إنشاء التوصيات بنجاح");
    }

    public async Task<OperationResult<IEnumerable<AIReport>>> GetStudentReportsAsync(int studentId, int? periodId = null)
    {
        var reports = await _unitOfWork.AIReports
            .FindAsync(r => r.StudentId == studentId && !r.IsDeleted);

        if (periodId.HasValue)
            reports = reports.Where(r => r.PeriodId == periodId).ToList();

        return OperationResult<IEnumerable<AIReport>>.Success(
            reports.OrderByDescending(r => r.CreatedAt));
    }

    public async Task<OperationResult<AIReport>> GetReportByIdAsync(int reportId, int userId, UserRole role)
    {
        var report = await _unitOfWork.AIReports.GetByIdAsync(reportId);
        if (report == null || report.IsDeleted)
            return OperationResult<AIReport>.Failure("التقرير غير موجود", 404);

        // Role-based access: Admin can see all, Teacher sees class reports, Parent sees their children
        if (!role.IsAdminLike())
        {
            if (role == UserRole.Teacher && !report.ClassId.HasValue)
                return OperationResult<AIReport>.Failure("لا يمكنك الوصول إلى هذا التقرير");

            if (role == UserRole.Parent)
            {
                var parentLinks = await _unitOfWork.ParentStudents
                    .FindAsync(ps => ps.ParentId == userId && ps.StudentId == report.StudentId);
                if (!parentLinks.Any())
                    return OperationResult<AIReport>.Failure("لا يمكنك الوصول إلى هذا التقرير");
            }
            else if (role == UserRole.Student && report.StudentId != userId)
            {
                return OperationResult<AIReport>.Failure("لا يمكنك الوصول إلى هذا التقرير");
            }
        }

        return OperationResult<AIReport>.Success(report);
    }
}
