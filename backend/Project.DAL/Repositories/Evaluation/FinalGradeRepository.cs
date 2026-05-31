using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class FinalGradeRepository : Repository<FinalGrade>, IFinalGradeRepository
{
    public FinalGradeRepository(AppDbContext context) : base(context) { }


    public async Task<FinalGrade?> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.FinalGrades
            .FirstOrDefaultAsync(fg => fg.EnrollmentId == enrollmentId, ct);


    public async Task<IReadOnlyList<FinalGrade>> GetByClassIdAsync(
        int classId,
        CancellationToken ct = default)
        => await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt  == null)
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(fg => fg.Total)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FinalGrade>> GetPublishedByClassIdAsync(
        int classId,
        CancellationToken ct = default)
        => await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId  &&
                fg.Enrollment.LeftAt  == null      &&
                fg.IsPublished)
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(fg => fg.Total)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<FinalGrade>> GetTopStudentsByClassAsync(
        int classId,
        int count,
        CancellationToken ct = default)
        => await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt  == null)
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(fg => fg.Total)
            .Take(count)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FinalGrade>> GetStudentsNeedingSupportAsync(
        int classId,
        decimal threshold,
        CancellationToken ct = default)
        => await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt  == null     &&
                fg.Total < threshold)
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(fg => fg.Total)
            .ToListAsync(ct);

    public async Task<decimal> GetClassAverageAsync(
        int classId,
        CancellationToken ct = default)
    {
        var result = await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt  == null)
            .Select(fg => (decimal?)fg.Total)
            .AverageAsync(ct);

        return result ?? 0m;
    }


    public async Task<IReadOnlyList<FinalGrade>> GetStudentsBelowThresholdAsync(
        int classId,
        decimal threshold,
        CancellationToken ct = default)
        => await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt  == null     &&
                fg.Total < threshold)
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(fg => fg.Total)
            .ToListAsync(ct);


    public async Task UpsertAsync(FinalGrade finalGrade, CancellationToken ct = default)
    {
        var existing = await _context.FinalGrades
            .FirstOrDefaultAsync(fg => fg.EnrollmentId == finalGrade.EnrollmentId, ct);

        if (existing is null)
            await _context.FinalGrades.AddAsync(finalGrade, ct);
        else
        {
            existing.PeriodAvgScore   = finalGrade.PeriodAvgScore;
            existing.Assessment1Score = finalGrade.Assessment1Score;
            existing.Assessment2Score = finalGrade.Assessment2Score;
            existing.WrittenTotal     = finalGrade.WrittenTotal;
            existing.FinalExamScore   = finalGrade.FinalExamScore;
            existing.Total            = finalGrade.Total;
            existing.UpdatedAt        = DateTime.UtcNow;
        }
    }

    public async Task BulkUpsertAsync(
        IEnumerable<FinalGrade> finalGrades,
        CancellationToken ct = default)
    {
        var list = finalGrades.ToList();
        if (!list.Any()) return;

        var enrollmentIds = list.Select(fg => fg.EnrollmentId).Distinct().ToList();

        var existing = await _context.FinalGrades
            .Where(fg => enrollmentIds.Contains(fg.EnrollmentId))
            .ToListAsync(ct);

        foreach (var grade in list)
        {
            var ex = existing.FirstOrDefault(fg => fg.EnrollmentId == grade.EnrollmentId);

            if (ex is null)
                await _context.FinalGrades.AddAsync(grade, ct);
            else
            {
                ex.PeriodAvgScore   = grade.PeriodAvgScore;
                ex.Assessment1Score = grade.Assessment1Score;
                ex.Assessment2Score = grade.Assessment2Score;
                ex.WrittenTotal     = grade.WrittenTotal;
                ex.FinalExamScore   = grade.FinalExamScore;
                ex.Total            = grade.Total;
                ex.UpdatedAt        = DateTime.UtcNow;
            }
        }
    }


    public async Task BulkPublishByClassAsync(int classId, CancellationToken ct = default)
        => await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt  == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(fg => fg.IsPublished, true)
                .SetProperty(fg => fg.UpdatedAt,   DateTime.UtcNow), ct);

    public async Task BulkUnpublishByClassAsync(int classId, CancellationToken ct = default)
        => await _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt  == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(fg => fg.IsPublished, false)
                .SetProperty(fg => fg.UpdatedAt,   DateTime.UtcNow), ct);
}



