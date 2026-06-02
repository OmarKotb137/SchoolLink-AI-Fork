using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IPeriodicAssessmentRepository : IRepository<PeriodicAssessment>
{
    Task<PeriodicAssessment?> GetByEnrollmentAndTypeAsync(int enrollmentId, PeriodicAssessmentType assessmentType, CancellationToken ct = default);

    Task<IReadOnlyList<PeriodicAssessment>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);
    Task<IReadOnlyList<PeriodicAssessment>> GetByEnrollmentAndTypesAsync(int enrollmentId, IEnumerable<PeriodicAssessmentType> types, CancellationToken ct = default);

    Task<IReadOnlyList<PeriodicAssessment>> GetByClassAndTypeAsync(int classId, PeriodicAssessmentType assessmentType, CancellationToken ct = default);

    Task UpsertAsync(PeriodicAssessment assessment, CancellationToken ct = default);
    Task BulkUpsertAsync(IEnumerable<PeriodicAssessment> assessments, CancellationToken ct = default);
}



