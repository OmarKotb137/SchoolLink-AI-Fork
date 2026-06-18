using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IStudentAssignmentSubmissionRepository : IRepository<StudentAssignmentSubmission>
{
    Task<StudentAssignmentSubmission?> GetByEnrollmentAndAssignmentAsync(int enrollmentId, int assignmentId, CancellationToken ct = default);
    Task<bool>                         HasSubmittedAsync(int enrollmentId, int assignmentId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentAssignmentSubmission>> GetByAssignmentIdAsync(int assignmentId, CancellationToken ct = default);
    Task<int>                                        GetSubmissionCountAsync(int assignmentId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentAssignmentSubmission>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);

    Task<IReadOnlyList<StudentAssignmentSubmission>> GetUngradedAsync(CancellationToken ct = default);
    Task<IReadOnlyList<StudentAssignmentSubmission>> GetUngradedByAssignmentAsync(int assignmentId, CancellationToken ct = default);

    Task<StudentAssignmentSubmission?> GetWithAnswersAsync(int submissionId, CancellationToken ct = default);
    Task<StudentAssignmentSubmission?> GetWithAnswersForEnrollmentAsync(int submissionId, int enrollmentId, CancellationToken ct = default);

    Task<decimal> GetAverageScoreAsync(int assignmentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentAssignmentSubmission>> GetPendingByTeacherAsync(int teacherId, int academicYearId, CancellationToken ct = default);
}



