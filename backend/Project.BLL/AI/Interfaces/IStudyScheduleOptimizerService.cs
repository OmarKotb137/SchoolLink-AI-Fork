using Common.Results;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface IStudyScheduleOptimizerService
{
    Task<OperationResult<List<object>>> OptimizeScheduleAsync(StudyPlanOptimizationRequest request, CancellationToken ct = default);
    Task<OperationResult<List<object>>> GetRecommendedScheduleAsync(int enrollmentId, CancellationToken ct = default);
}
