using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<IReadOnlyList<Assignment>> GetByClassSubjectTeacherIdAsync(int classSubjectTeacherId, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetByCategoryAsync(int classSubjectTeacherId, EvaluationCategory category, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetAIGeneratedAsync(int classSubjectTeacherId, CancellationToken ct = default);

    Task<IReadOnlyList<Assignment>> GetByClassIdAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetUpcomingByClassAsync(int classId, int days, CancellationToken ct = default);

    Task<IReadOnlyList<Assignment>> GetPendingForEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetOverdueForEnrollmentAsync(int enrollmentId, CancellationToken ct = default);

    Task<IReadOnlyList<Assignment>> GetByDueDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetOverdueAsync(int? classSubjectTeacherId = null, CancellationToken ct = default);

    Task<Assignment?> GetWithQuestionsAsync(int assignmentId, CancellationToken ct = default);
    Task<IReadOnlyList<Assignment>> GetPublishedForEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
    Task<Assignment?> GetStudentAssignmentDetailsAsync(int assignmentId, int enrollmentId, CancellationToken ct = default);
}



