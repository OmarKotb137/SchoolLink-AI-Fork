using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.StudyPlans;

public interface IStudyPlanItemRepository : IRepository<StudyPlanItem>
{
    Task<IReadOnlyList<StudyPlanItem>> GetByStudyPlanIdAsync(int studyPlanId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanItem>> GetByStudyPlanAndDayAsync(int studyPlanId, SchoolDay day, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanItem>> GetBySubjectAsync(int studyPlanId, int subjectId, CancellationToken ct = default);

    Task<IReadOnlyList<StudyPlanItem>> GetIncompleteByStudyPlanAsync(int studyPlanId, CancellationToken ct = default);
    Task<IReadOnlyList<StudyPlanItem>> GetCompletedByStudyPlanAsync(int studyPlanId, CancellationToken ct = default);

    Task MarkAsCompletedAsync(int itemId, CancellationToken ct = default);
    Task MarkAsIncompleteAsync(int itemId, CancellationToken ct = default);

    Task<int>     GetCompletedCountAsync(int studyPlanId, CancellationToken ct = default);
    Task<int>     GetTotalCountAsync(int studyPlanId, CancellationToken ct = default);
    Task<decimal> GetCompletionRateAsync(int studyPlanId, CancellationToken ct = default);   // completed / total * 100

    Task BulkReplaceAsync(int studyPlanId, IEnumerable<StudyPlanItem> items, CancellationToken ct = default);
}



