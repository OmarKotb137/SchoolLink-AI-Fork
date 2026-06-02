using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IEvaluationPeriodRepository : IRepository<EvaluationPeriod>
{
    Task<IReadOnlyList<EvaluationPeriod>> GetByAcademicYearAsync(int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<EvaluationPeriod>> GetWeeksByYearAsync(int academicYearId, CancellationToken ct = default);        // PeriodType = Weekly
    Task<IReadOnlyList<EvaluationPeriod>> GetByTypeAndYearAsync(int academicYearId, PeriodType periodType, CancellationToken ct = default);
    Task<IReadOnlyList<EvaluationPeriod>> GetOrderedByYearAsync(int academicYearId, CancellationToken ct = default);

    Task<IReadOnlyList<EvaluationPeriod>> GetByMonthNameAsync(int academicYearId, string monthName, CancellationToken ct = default);
    Task<IReadOnlyList<string>>           GetDistinctMonthNamesAsync(int academicYearId, CancellationToken ct = default);

    Task<EvaluationPeriod?> GetCurrentWeekAsync(int academicYearId, CancellationToken ct = default);
    Task<EvaluationPeriod?> GetByDateAsync(int academicYearId, DateOnly date, CancellationToken ct = default);
}



