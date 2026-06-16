using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IStudentExamAttemptRepository : IRepository<StudentExamAttempt>
{
    Task<StudentExamAttempt?> GetByEnrollmentAndExamAsync(int enrollmentId, int examId, CancellationToken ct = default);
    Task<bool>                HasAttemptedAsync(int enrollmentId, int examId, CancellationToken ct = default);
    Task<StudentExamAttempt?> GetActiveAttemptAsync(int enrollmentId, int examId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentExamAttempt>> GetByExamIdAsync(int examId, CancellationToken ct = default);
    Task<int>                               GetAttemptCountAsync(int examId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentExamAttempt>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentExamAttempt>> GetUngradedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudentExamAttempt>> GetUngradedByExamAsync(int examId, CancellationToken ct = default);

    Task<StudentExamAttempt?> GetWithAnswersAsync(int attemptId, CancellationToken ct = default);
    Task<StudentExamAttempt?> GetWithAnswersForEnrollmentAsync(int attemptId, int enrollmentId, CancellationToken ct = default);

    Task<decimal>                           GetAverageScoreAsync(int examId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentExamAttempt>> GetTopScorersAsync(int examId, int count, CancellationToken ct = default);
}



