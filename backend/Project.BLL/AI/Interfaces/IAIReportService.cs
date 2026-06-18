using Common.Results;
using Project.BLL.DTOs.Reports;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.AI.Interfaces;

public interface IAIReportService
{
    Task<OperationResult<AIReport>> GenerateStudentReportAsync(int studentId, int periodId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AIReport>> GenerateClassReportAsync(int classId, int periodId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AIReport>> GenerateRecommendationsAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<IEnumerable<AIReport>>> GetStudentReportsAsync(int studentId, int? periodId = null);
    Task<OperationResult<AIReport>> GetReportByIdAsync(int reportId, int userId, UserRole role);

    /// <summary>
    /// Returns structured student report data by querying the database (FinalGrade, evaluations)
    /// and generating AI text for the analysis.
    /// </summary>
    Task<OperationResult<StudentReportDto>> GetStructuredStudentReportAsync(int studentId, int periodId, AcademicTerm? term = null, CancellationToken ct = default);

    /// <summary>
    /// Returns structured recommendations data with AI-generated text.
    /// </summary>
    Task<OperationResult<RecommendationsDto>> GetStructuredRecommendationsAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a report by ID (with role-based access check).
    /// </summary>
    Task<OperationResult<object>> DeleteReportAsync(int reportId, int userId, UserRole role);
}
