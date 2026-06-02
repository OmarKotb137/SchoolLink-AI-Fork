using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Evaluation;

public interface IEvaluationItemRepository : IRepository<EvaluationItem>
{
    Task<IReadOnlyList<EvaluationItem>> GetByTemplateIdAsync(int templateId, CancellationToken ct = default);
    Task<IReadOnlyList<EvaluationItem>> GetVisibleByTemplateIdAsync(int templateId, CancellationToken ct = default);    // IsVisible = true
    Task<IReadOnlyList<EvaluationItem>> GetOrderedByTemplateIdAsync(int templateId, CancellationToken ct = default);
    Task<decimal>                       GetMaxTotalScoreByTemplateAsync(int templateId, CancellationToken ct = default);
    Task<int>                           GetItemCountByTemplateAsync(int templateId, CancellationToken ct = default);
}



