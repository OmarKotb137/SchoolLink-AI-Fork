using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IExamRepository : IRepository<Exam>
{
    Task<IReadOnlyList<Exam>> GetByClassSubjectTeacherIdAsync(int classSubjectTeacherId, CancellationToken ct = default);
    Task<IReadOnlyList<Exam>> GetByCategoryAsync(int classSubjectTeacherId, EvaluationCategory category, CancellationToken ct = default);
    Task<IReadOnlyList<Exam>> GetAIGeneratedAsync(int classSubjectTeacherId, CancellationToken ct = default);

    Task<IReadOnlyList<Exam>> GetPublishedByClassIdAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<IReadOnlyList<Exam>> GetUpcomingByClassAsync(int classId, int days, CancellationToken ct = default);

    Task<IReadOnlyList<Exam>> GetActiveExamsAsync(CancellationToken ct = default);                       // StartTime <= now <= EndTime
    Task<IReadOnlyList<Exam>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken ct = default);

    Task<IReadOnlyList<Exam>> GetByAcademicYearAsync(int academicYearId, CancellationToken ct = default);

    Task<Exam?> GetWithQuestionsAsync(int examId, CancellationToken ct = default);
    Task<Exam?> GetWithClassSubjectTeacherAsync(int examId, CancellationToken ct = default);
    Task<IReadOnlyList<Exam>> GetPublishedForEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
    Task<Exam?> GetStudentExamDetailsAsync(int examId, int enrollmentId, CancellationToken ct = default);
    Task<Exam?> GetByUidAsync(Guid uid, CancellationToken ct = default);
    Task<Exam?> GetWithQuestionsByUidAsync(Guid uid, CancellationToken ct = default);
    Task<IReadOnlyList<Exam>> GetAIGeneratedByTeacherAsync(List<int> cstIds, CancellationToken ct = default);
}



