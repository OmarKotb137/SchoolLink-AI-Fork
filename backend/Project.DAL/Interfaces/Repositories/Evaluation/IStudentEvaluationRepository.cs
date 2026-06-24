using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IStudentEvaluationRepository : IRepository<StudentEvaluation>
{
    Task<StudentEvaluation?> GetByEnrollmentItemAndPeriodAsync(int enrollmentId, int evaluationItemId, int periodId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentEvaluation>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEvaluation>> GetByEnrollmentAndPeriodAsync(int enrollmentId, int periodId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEvaluation>> GetByEnrollmentAndItemAsync(int enrollmentId, int evaluationItemId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentEvaluation>> GetByPeriodAndClassAsync(int periodId, int classId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEvaluation>> GetByPeriodAndEnrollmentsAsync(int periodId, IEnumerable<int> enrollmentIds, CancellationToken ct = default);
    Task<IReadOnlyList<StudentEvaluation>> GetByPeriodsAndEnrollmentsAsync(IEnumerable<int> periodIds, IEnumerable<int> enrollmentIds, CancellationToken ct = default);

    Task<decimal> GetWeeklyTotalScoreAsync(int enrollmentId, int periodId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentEvaluation>> GetByEnteredByAsync(int userId, CancellationToken ct = default);

    Task BulkUpsertAsync(IEnumerable<StudentEvaluation> evaluations, CancellationToken ct = default);
}



