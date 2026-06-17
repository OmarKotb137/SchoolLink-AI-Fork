using Common.Results;
using Project.Domain.Enums;

namespace Project.BLL.AI.Interfaces;

public interface IEvaluationReportService
{
    Task<OperationResult<string>> GenerateStudentReportAsync(int studentId, int periodId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<string>> GenerateClassReportAsync(int classId, int periodId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<string>> GenerateRecommendationsAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default);
}
