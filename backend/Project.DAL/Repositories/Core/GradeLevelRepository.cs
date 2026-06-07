using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class GradeLevelRepository : Repository<GradeLevel>, IGradeLevelRepository
{
    public GradeLevelRepository(AppDbContext context) : base(context) { }

    public async Task<GradeLevel?> GetByNameAsync(
        string name,
        CancellationToken ct = default)
        => await _context.GradeLevels
            .FirstOrDefaultAsync(g => g.Name == name, ct);

    public async Task<GradeLevel?> GetByLevelOrderAsync(
        int levelOrder,
        CancellationToken ct = default)
        => await _context.GradeLevels
            .FirstOrDefaultAsync(g => g.LevelOrder == levelOrder, ct);

    public async Task<GradeLevel?> GetByLevelOrderExcludingIdAsync(
        int levelOrder,
        int excludedId,
        CancellationToken ct = default)
        => await _context.GradeLevels
            .FirstOrDefaultAsync(g => g.LevelOrder == levelOrder && g.Id != excludedId, ct);

    public async Task<IReadOnlyList<GradeLevel>> GetByStageAsync(
        string stage,
        CancellationToken ct = default)
        => await _context.GradeLevels
            .Where(g => g.Stage == stage)
            .OrderBy(g => g.LevelOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<GradeLevel>> GetAllOrderedAsync(
        CancellationToken ct = default)
        => await _context.GradeLevels
            .OrderBy(g => g.LevelOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetDistinctStagesAsync(
        CancellationToken ct = default)
        => await _context.GradeLevels
            .Where(g => g.Stage != null)
            .Select(g => g.Stage!)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);
}



