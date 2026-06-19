using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IFinalGradeRepository : IRepository<FinalGrade>
{
    Task<FinalGrade?> GetByEnrollmentIdAsync(int enrollmentId, AcademicTerm? term = null, int? subjectId = null, CancellationToken ct = default);

    Task<IReadOnlyList<FinalGrade>> GetByClassIdAsync(int classId, AcademicTerm? term = null, int? subjectId = null, CancellationToken ct = default);
    Task<IReadOnlyList<FinalGrade>> GetPublishedByClassIdAsync(int classId, AcademicTerm? term = null, CancellationToken ct = default);

    Task<IReadOnlyList<FinalGrade>> GetTopStudentsByClassAsync(int classId, int count, AcademicTerm? term = null, CancellationToken ct = default);
    Task<IReadOnlyList<FinalGrade>> GetStudentsNeedingSupportAsync(int classId, decimal threshold, AcademicTerm? term = null, CancellationToken ct = default);
    Task<decimal>                   GetClassAverageAsync(int classId, AcademicTerm? term = null, CancellationToken ct = default);

    Task<IReadOnlyList<FinalGrade>> GetStudentsBelowThresholdAsync(int classId, decimal threshold, AcademicTerm? term = null, CancellationToken ct = default);

    Task UpsertAsync(FinalGrade finalGrade, CancellationToken ct = default);
    Task BulkUpsertAsync(IEnumerable<FinalGrade> finalGrades, CancellationToken ct = default);
    Task BulkPublishByClassAsync(int classId, AcademicTerm? term = null, CancellationToken ct = default);
    Task BulkUnpublishByClassAsync(int classId, AcademicTerm? term = null, CancellationToken ct = default);
}
