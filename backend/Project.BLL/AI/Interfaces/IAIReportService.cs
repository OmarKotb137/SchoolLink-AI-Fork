using Common.Results;
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
}
