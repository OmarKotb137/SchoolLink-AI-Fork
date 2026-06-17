using Common.Results;
using Project.BLL.DTOs.EvaluationPeriods;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IEvaluationPeriodService
{
    Task<OperationResult<EvaluationPeriodDto>> CreateEvaluationPeriodAsync(CreateEvaluationPeriodRequest request);
    Task<OperationResult<EvaluationPeriodDto>> UpdateEvaluationPeriodAsync(UpdateEvaluationPeriodRequest request);
    Task<OperationResult> DeleteEvaluationPeriodAsync(int id);
    Task<OperationResult<IEnumerable<EvaluationPeriodDto>>> GetPeriodsByAcademicYearAsync(int academicYearId, PeriodType? type = null);
    Task<OperationResult<EvaluationPeriodDto>> GetCurrentWeekAsync(int academicYearId);
    Task<OperationResult<IEnumerable<string>>> GetDistinctMonthNamesAsync(int academicYearId);
    Task<OperationResult<IEnumerable<EvaluationPeriodDto>>> GetPeriodsByMonthAsync(int academicYearId, string monthName);
    Task<OperationResult<EvaluationPeriodDto>> GetPeriodByIdAsync(int id);
    Task<OperationResult<EvaluationPeriodDto>> GetActivePeriodAsync(int academicYearId);
    Task<OperationResult<EvaluationPeriodDto>> GetCurrentTermAsync(int academicYearId);
}
