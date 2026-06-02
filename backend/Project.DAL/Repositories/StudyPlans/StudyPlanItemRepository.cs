using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.StudyPlans;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.StudyPlans;

public class StudyPlanItemRepository : Repository<StudyPlanItem>, IStudyPlanItemRepository
{
    public StudyPlanItemRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<StudyPlanItem>> GetByStudyPlanIdAsync(
        int studyPlanId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .Where(i => i.StudyPlanId == studyPlanId)
            .Include(i => i.Subject)
            .OrderBy(i => i.DayOfWeek)
            .ThenBy(i => i.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlanItem>> GetByStudyPlanAndDayAsync(
        int studyPlanId,
        SchoolDay day,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .Where(i =>
                i.StudyPlanId == studyPlanId &&
                i.DayOfWeek   == day)
            .Include(i => i.Subject)
            .OrderBy(i => i.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlanItem>> GetBySubjectAsync(
        int studyPlanId,
        int subjectId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .Where(i =>
                i.StudyPlanId == studyPlanId &&
                i.SubjectId   == subjectId)
            .Include(i => i.Subject)
            .OrderBy(i => i.DayOfWeek)
            .ThenBy(i => i.StartTime)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<StudyPlanItem>> GetIncompleteByStudyPlanAsync(
        int studyPlanId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .Where(i =>
                i.StudyPlanId == studyPlanId &&
                !i.IsCompleted)
            .Include(i => i.Subject)
            .OrderBy(i => i.DayOfWeek)
            .ThenBy(i => i.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlanItem>> GetCompletedByStudyPlanAsync(
        int studyPlanId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .Where(i =>
                i.StudyPlanId == studyPlanId &&
                i.IsCompleted)
            .Include(i => i.Subject)
            .OrderBy(i => i.DayOfWeek)
            .ThenBy(i => i.StartTime)
            .ToListAsync(ct);


    public async Task MarkAsCompletedAsync(
        int itemId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.IsCompleted, true)
                .SetProperty(i => i.UpdatedAt,   DateTime.UtcNow), ct);

    public async Task MarkAsIncompleteAsync(
        int itemId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .Where(i => i.Id == itemId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.IsCompleted, false)
                .SetProperty(i => i.UpdatedAt,   DateTime.UtcNow), ct);


    public async Task<int> GetCompletedCountAsync(
        int studyPlanId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .CountAsync(i =>
                i.StudyPlanId == studyPlanId &&
                i.IsCompleted, ct);

    public async Task<int> GetTotalCountAsync(
        int studyPlanId,
        CancellationToken ct = default)
        => await _context.StudyPlanItems
            .CountAsync(i => i.StudyPlanId == studyPlanId, ct);

    public async Task<decimal> GetCompletionRateAsync(
        int studyPlanId,
        CancellationToken ct = default)
    {
        var total = await _context.StudyPlanItems
            .CountAsync(i => i.StudyPlanId == studyPlanId, ct);

        if (total == 0) return 0m;

        var completed = await _context.StudyPlanItems
            .CountAsync(i =>
                i.StudyPlanId == studyPlanId &&
                i.IsCompleted, ct);

        return Math.Round((decimal)completed / total * 100, 2);
    }


    public async Task BulkReplaceAsync(
        int studyPlanId,
        IEnumerable<StudyPlanItem> items,
        CancellationToken ct = default)
    {
        var existing = await _context.StudyPlanItems
            .Where(i => i.StudyPlanId == studyPlanId)
            .ToListAsync(ct);

        foreach (var item in existing)
            SoftDelete(item);

        await _context.StudyPlanItems.AddRangeAsync(items, ct);
    }
}



