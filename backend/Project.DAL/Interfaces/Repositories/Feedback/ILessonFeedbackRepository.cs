using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Feedback;

public interface ILessonFeedbackRepository : IRepository<LessonFeedback>
{
    Task<IReadOnlyList<LessonFeedback>> GetByClassSubjectTeacherIdAsync(int classSubjectTeacherId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonFeedback>> GetByLessonDateAsync(int classSubjectTeacherId, DateOnly lessonDate, CancellationToken ct = default);
    Task<IReadOnlyList<LessonFeedback>> GetByDateRangeAsync(int classSubjectTeacherId, DateOnly from, DateOnly to, CancellationToken ct = default);

    Task<IReadOnlyList<LessonFeedback>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);

    Task<bool> HasFeedbackAsync(int enrollmentId, int classSubjectTeacherId, DateOnly lessonDate, CancellationToken ct = default);

    Task<decimal>                          GetAverageRatingAsync(int classSubjectTeacherId, CancellationToken ct = default);
    Task<decimal>                          GetAverageRatingByDateRangeAsync(int classSubjectTeacherId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<Dictionary<LessonUnderstanding, int>> GetUnderstandingStatsAsync(int classSubjectTeacherId, DateOnly lessonDate, CancellationToken ct = default);
    Task<int>                              GetFeedbackCountByLessonAsync(int classSubjectTeacherId, DateOnly lessonDate, CancellationToken ct = default);
    Task<decimal>                          GetOverallTeacherRatingAsync(int teacherId, int academicYearId, CancellationToken ct = default);
}



