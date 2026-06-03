using Common.Results;
using Project.BLL.DTOs.PeriodAverages;

namespace Project.BLL.Interfaces;

public interface IPeriodAverageService
{
    Task<OperationResult<PeriodAverageDto>> CalculateAndSaveAsync(CalculatePeriodAverageRequest request);
    Task<OperationResult<IEnumerable<PeriodAverageDto>>> GetByEnrollmentAsync(int enrollmentId);
    Task<OperationResult<IEnumerable<PeriodAverageDto>>> GetByClassAndPeriodAsync(int classId, int periodId);
    Task<OperationResult> DeletePeriodAverageAsync(int id);
    Task<OperationResult<PeriodAverageDto>> GetPeriodAverageByIdAsync(int id);
    Task<OperationResult<int>> CalculateAllForClassAsync(int classId, int periodId);
}
