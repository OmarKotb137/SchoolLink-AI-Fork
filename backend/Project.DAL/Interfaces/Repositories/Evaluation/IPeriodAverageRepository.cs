using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IPeriodAverageRepository : IRepository<PeriodAverage>
{
    Task<PeriodAverage?> GetByEnrollmentAndPeriodAsync(int enrollmentId, int periodId, CancellationToken ct = default);

    Task<IReadOnlyList<PeriodAverage>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);

    Task<IReadOnlyList<PeriodAverage>> GetByClassAndPeriodAsync(int classId, int periodId, CancellationToken ct = default);
    Task<IReadOnlyList<PeriodAverage>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<decimal> GetOverallAverageForEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
    Task<decimal> GetClassAverageForPeriodAsync(int classId, int periodId, CancellationToken ct = default);

    Task<(decimal Current, decimal Previous)?> GetCurrentAndPreviousAveragesAsync(int enrollmentId, int currentPeriodId, int previousPeriodId, CancellationToken ct = default);

    Task   UpsertAsync(PeriodAverage periodAverage, CancellationToken ct = default);
    Task   BulkUpsertAsync(IEnumerable<PeriodAverage> periodAverages, CancellationToken ct = default);
}



