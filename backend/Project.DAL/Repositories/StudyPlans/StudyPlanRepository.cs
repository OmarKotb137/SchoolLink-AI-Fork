using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.StudyPlans;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.StudyPlans;

public class StudyPlanRepository : Repository<StudyPlan>, IStudyPlanRepository
{
    public StudyPlanRepository(AppDbContext context) : base(context) { }


    public async Task<StudyPlan?> GetActiveByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudyPlans
            .Include(sp => sp.Items
                .OrderBy(i => i.DayOfWeek)
                .ThenBy(i => i.StartTime))
                .ThenInclude(i => i.Subject)
            .FirstOrDefaultAsync(sp =>
                sp.EnrollmentId == enrollmentId &&
                sp.IsActive, ct);

    public async Task<bool> HasActivePlanAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudyPlans
            .AnyAsync(sp =>
                sp.EnrollmentId == enrollmentId &&
                sp.IsActive, ct);


    public async Task<IReadOnlyList<StudyPlan>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudyPlans
            .Where(sp => sp.EnrollmentId == enrollmentId)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudyPlan>> GetAIGeneratedByEnrollmentAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudyPlans
            .Where(sp =>
                sp.EnrollmentId  == enrollmentId &&
                sp.GeneratedByAI)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync(ct);


    public async Task<StudyPlan?> GetWithItemsAsync(
        int studyPlanId,
        CancellationToken ct = default)
        => await _context.StudyPlans
            .Include(sp => sp.Items
                .OrderBy(i => i.DayOfWeek)
                .ThenBy(i => i.StartTime))
                .ThenInclude(i => i.Subject)
            .FirstOrDefaultAsync(sp => sp.Id == studyPlanId, ct);


    public async Task DeactivateByEnrollmentAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudyPlans
            .Where(sp =>
                sp.EnrollmentId == enrollmentId &&
                sp.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(sp => sp.IsActive,  false)
                .SetProperty(sp => sp.UpdatedAt, DateTime.UtcNow), ct);
}



