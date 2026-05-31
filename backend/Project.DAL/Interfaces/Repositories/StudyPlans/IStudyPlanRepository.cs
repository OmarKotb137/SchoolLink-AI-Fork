using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.StudyPlans;

public interface IStudyPlanRepository : IRepository<StudyPlan>
{
    Task<StudyPlan?> GetActiveByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);
    Task<bool>       HasActivePlanAsync(int enrollmentId, CancellationToken ct = default);

    Task<IReadOnlyList<StudyPlan>> GetByEnrollmentIdAsync(int enrollmentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlan>> GetAIGeneratedByEnrollmentAsync(int enrollmentId, CancellationToken ct = default);

    Task<StudyPlan?> GetWithItemsAsync(int studyPlanId, CancellationToken ct = default);

    Task DeactivateByEnrollmentAsync(int enrollmentId, CancellationToken ct = default);
}



