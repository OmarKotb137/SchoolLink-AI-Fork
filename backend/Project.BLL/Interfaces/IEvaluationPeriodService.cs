using Common.Results;
using Project.BLL.DTOs.EvaluationPeriods;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IEvaluationPeriodService
{
    Task<OperationResult<EvaluationPeriodDto>> CreateEvaluationPeriodAsync(CreateEvaluationPeriodRequest request);
    Task<OperationResult<IEnumerable<EvaluationPeriodDto>>> GetPeriodsByAcademicYearAsync(int academicYearId, PeriodType? type = null);
}
