using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class StudentEvaluationRepository : Repository<StudentEvaluation>, IStudentEvaluationRepository
{
    public StudentEvaluationRepository(AppDbContext context) : base(context) { }


    /// <summary>
    /// UNIQUE: EnrollmentId + EvaluationItemId + PeriodId.
    /// </summary>
    public async Task<StudentEvaluation?> GetByEnrollmentItemAndPeriodAsync(
        int enrollmentId,
        int evaluationItemId,
        int periodId,
        CancellationToken ct = default)
        => await _context.StudentEvaluations
            .FirstOrDefaultAsync(se =>
                se.EnrollmentId == enrollmentId &&
                se.EvaluationItemId == evaluationItemId &&
                se.PeriodId == periodId, ct);


    public async Task<IReadOnlyList<StudentEvaluation>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudentEvaluations
            .Where(se => se.EnrollmentId == enrollmentId)
            .Include(se => se.EvaluationItem)
            .Include(se => se.Period)
            .OrderBy(se => se.Period.OrderNum)
            .ThenBy(se => se.EvaluationItem.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentEvaluation>> GetByEnrollmentAndPeriodAsync(
        int enrollmentId,
        int periodId,
        CancellationToken ct = default)
        => await _context.StudentEvaluations
            .Where(se =>
                se.EnrollmentId == enrollmentId &&
                se.PeriodId == periodId)
            .Include(se => se.EvaluationItem)
                .ThenInclude(e => e.Template)
                    .ThenInclude(t => t.Subject)
            .Include(se => se.Period)
            .OrderBy(se => se.EvaluationItem.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentEvaluation>> GetByEnrollmentAndItemAsync(
        int enrollmentId,
        int evaluationItemId,
        CancellationToken ct = default)
        => await _context.StudentEvaluations
            .Where(se =>
                se.EnrollmentId == enrollmentId &&
                se.EvaluationItemId == evaluationItemId)
            .Include(se => se.Period)
            .OrderBy(se => se.Period.OrderNum)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<StudentEvaluation>> GetByPeriodAndClassAsync(
        int periodId,
        int classId,
        CancellationToken ct = default)
        => await _context.StudentEvaluations
            .Where(se =>
                se.PeriodId == periodId &&
                se.Enrollment.ClassId == classId)
            .Include(se => se.Enrollment)
                .ThenInclude(e => e.Student)
            .Include(se => se.EvaluationItem)
            .OrderBy(se => se.Enrollment.Student.FullName)
            .ThenBy(se => se.EvaluationItem.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentEvaluation>> GetByPeriodAndEnrollmentsAsync(
        int periodId,
        IEnumerable<int> enrollmentIds,
        CancellationToken ct = default)
    {
        var ids = enrollmentIds.ToList();
        return await _context.StudentEvaluations
            .Where(se =>
                se.PeriodId == periodId &&
                ids.Contains(se.EnrollmentId))
            .Include(se => se.EvaluationItem)
            .Include(se => se.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(se => se.Enrollment.Student.FullName)
            .ThenBy(se => se.EvaluationItem.DisplayOrder)
            .ToListAsync(ct);
    }


    public async Task<decimal> GetWeeklyTotalScoreAsync(
        int enrollmentId,
        int periodId,
        CancellationToken ct = default)
        => await _context.StudentEvaluations
            .Where(se =>
                se.EnrollmentId == enrollmentId &&
                se.PeriodId == periodId &&
                se.Score.HasValue)
            .SumAsync(se => se.Score!.Value, ct);


    public async Task<IReadOnlyList<StudentEvaluation>> GetByEnteredByAsync(
        int userId,
        CancellationToken ct = default)
        => await _context.StudentEvaluations
            .Where(se => se.EnteredById == userId)
            .Include(se => se.Enrollment)
                .ThenInclude(e => e.Student)
            .Include(se => se.Period)
            .Include(se => se.EvaluationItem)
            .OrderByDescending(se => se.EnteredAt)
            .ToListAsync(ct);


    public async Task BulkUpsertAsync(
        IEnumerable<StudentEvaluation> evaluations,
        CancellationToken ct = default)
    {
        var evaluationList = evaluations.ToList();
        if (!evaluationList.Any()) return;

        var enrollmentIds = evaluationList.Select(e => e.EnrollmentId).Distinct().ToList();
        var periodIds     = evaluationList.Select(e => e.PeriodId).Distinct().ToList();
        var itemIds       = evaluationList.Select(e => e.EvaluationItemId).Distinct().ToList();

        var existingEvaluations = await _context.StudentEvaluations
            .Where(se =>
                enrollmentIds.Contains(se.EnrollmentId) &&
                periodIds.Contains(se.PeriodId) &&
                itemIds.Contains(se.EvaluationItemId))
            .ToListAsync(ct);

        foreach (var evaluation in evaluationList)
        {
            var existing = existingEvaluations.FirstOrDefault(se =>
                se.EnrollmentId     == evaluation.EnrollmentId &&
                se.EvaluationItemId == evaluation.EvaluationItemId &&
                se.PeriodId         == evaluation.PeriodId);

            if (existing is null)
            {
                await _context.StudentEvaluations.AddAsync(evaluation, ct);
            }
            else
            {
                existing.Score       = evaluation.Score;
                existing.EnteredById = evaluation.EnteredById;
                existing.EnteredAt   = evaluation.EnteredAt;
                existing.UpdatedAt   = DateTime.UtcNow;
            }
        }
    }
}



