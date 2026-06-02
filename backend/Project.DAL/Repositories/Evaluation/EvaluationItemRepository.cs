using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class EvaluationItemRepository : Repository<EvaluationItem>, IEvaluationItemRepository
{
    public EvaluationItemRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<EvaluationItem>> GetByTemplateIdAsync(
        int templateId,
        CancellationToken ct = default)
        => await _context.EvaluationItems
            .Where(i => i.TemplateId == templateId)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvaluationItem>> GetVisibleByTemplateIdAsync(
        int templateId,
        CancellationToken ct = default)
        => await _context.EvaluationItems
            .Where(i => i.TemplateId == templateId && i.IsVisible)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvaluationItem>> GetOrderedByTemplateIdAsync(
        int templateId,
        CancellationToken ct = default)
        => await _context.EvaluationItems
            .Where(i => i.TemplateId == templateId && i.IsVisible)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync(ct);

    public async Task<decimal> GetMaxTotalScoreByTemplateAsync(
        int templateId,
        CancellationToken ct = default)
        => await _context.EvaluationItems
            .Where(i => i.TemplateId == templateId && i.IsVisible)
            .SumAsync(i => i.MaxScore, ct);

    public async Task<int> GetItemCountByTemplateAsync(
        int templateId,
        CancellationToken ct = default)
        => await _context.EvaluationItems
            .CountAsync(i => i.TemplateId == templateId, ct);
}



