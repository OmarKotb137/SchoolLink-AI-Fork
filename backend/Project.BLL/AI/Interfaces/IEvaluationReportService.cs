using Common.Results;

namespace Project.BLL.AI.Interfaces;

public interface IEvaluationReportService
{
    Task<OperationResult<string>> GenerateStudentReportAsync(int studentId, int periodId, CancellationToken ct = default);
    Task<OperationResult<string>> GenerateClassReportAsync(int classId, int periodId, CancellationToken ct = default);
    Task<OperationResult<string>> GenerateRecommendationsAsync(int studentId, CancellationToken ct = default);
}
