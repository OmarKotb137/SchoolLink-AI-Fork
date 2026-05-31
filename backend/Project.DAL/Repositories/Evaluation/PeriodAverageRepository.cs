using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class PeriodAverageRepository : Repository<PeriodAverage>, IPeriodAverageRepository
{
    public PeriodAverageRepository(AppDbContext context) : base(context) { }


    public async Task<PeriodAverage?> GetByEnrollmentAndPeriodAsync(
        int enrollmentId,
        int periodId,
        CancellationToken ct = default)
        => await _context.PeriodAverages
            .FirstOrDefaultAsync(pa =>
                pa.EnrollmentId == enrollmentId &&
                pa.PeriodId     == periodId, ct);


    public async Task<IReadOnlyList<PeriodAverage>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.PeriodAverages
            .Where(pa => pa.EnrollmentId == enrollmentId)
            .Include(pa => pa.Period)
            .OrderBy(pa => pa.Period.OrderNum)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<PeriodAverage>> GetByClassAndPeriodAsync(
        int classId,
        int periodId,
        CancellationToken ct = default)
        => await _context.PeriodAverages
            .Where(pa =>
                pa.PeriodId              == periodId &&
                pa.Enrollment.ClassId    == classId  &&
                pa.Enrollment.LeftAt     == null)
            .Include(pa => pa.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(pa => pa.AvgScore)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PeriodAverage>> GetByClassAndYearAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.PeriodAverages
            .Where(pa =>
                pa.Enrollment.ClassId        == classId        &&
                pa.Enrollment.AcademicYearId == academicYearId &&
                pa.Enrollment.LeftAt         == null)
            .Include(pa => pa.Enrollment)
                .ThenInclude(e => e.Student)
            .Include(pa => pa.Period)
            .OrderBy(pa => pa.Enrollment.Student.FullName)
            .ThenBy(pa => pa.Period.OrderNum)
            .ToListAsync(ct);


    public async Task<decimal> GetOverallAverageForEnrollmentAsync(
        int enrollmentId,
        CancellationToken ct = default)
    {
        var result = await _context.PeriodAverages
            .Where(pa => pa.EnrollmentId == enrollmentId)
            .Select(pa => (decimal?)pa.AvgScore)
            .AverageAsync(ct);

        return result ?? 0m;
    }

    public async Task<decimal> GetClassAverageForPeriodAsync(
        int classId,
        int periodId,
        CancellationToken ct = default)
    {
        var result = await _context.PeriodAverages
            .Where(pa =>
                pa.PeriodId           == periodId &&
                pa.Enrollment.ClassId == classId  &&
                pa.Enrollment.LeftAt  == null)
            .Select(pa => (decimal?)pa.AvgScore)
            .AverageAsync(ct);

        return result ?? 0m;
    }


    public async Task<(decimal Current, decimal Previous)?> GetCurrentAndPreviousAveragesAsync(
        int enrollmentId,
        int currentPeriodId,
        int previousPeriodId,
        CancellationToken ct = default)
    {
        var averages = await _context.PeriodAverages
            .Where(pa =>
                pa.EnrollmentId == enrollmentId &&
                (pa.PeriodId == currentPeriodId || pa.PeriodId == previousPeriodId))
            .ToListAsync(ct);

        var current  = averages.FirstOrDefault(pa => pa.PeriodId == currentPeriodId);
        var previous = averages.FirstOrDefault(pa => pa.PeriodId == previousPeriodId);

        if (current is null || previous is null) return null;

        return (current.AvgScore, previous.AvgScore);
    }


    public async Task UpsertAsync(PeriodAverage periodAverage, CancellationToken ct = default)
    {
        var existing = await _context.PeriodAverages
            .FirstOrDefaultAsync(pa =>
                pa.EnrollmentId == periodAverage.EnrollmentId &&
                pa.PeriodId     == periodAverage.PeriodId, ct);

        if (existing is null)
            await _context.PeriodAverages.AddAsync(periodAverage, ct);
        else
        {
            existing.AvgScore     = periodAverage.AvgScore;
            existing.MaxScore     = periodAverage.MaxScore;
            existing.CalculatedAt = periodAverage.CalculatedAt;
            existing.UpdatedAt    = DateTime.UtcNow;
        }
    }

    public async Task BulkUpsertAsync(
        IEnumerable<PeriodAverage> periodAverages,
        CancellationToken ct = default)
    {
        var list = periodAverages.ToList();
        if (!list.Any()) return;

        var enrollmentIds = list.Select(pa => pa.EnrollmentId).Distinct().ToList();
        var periodIds     = list.Select(pa => pa.PeriodId).Distinct().ToList();

        var existing = await _context.PeriodAverages
            .Where(pa =>
                enrollmentIds.Contains(pa.EnrollmentId) &&
                periodIds.Contains(pa.PeriodId))
            .ToListAsync(ct);

        foreach (var avg in list)
        {
            var ex = existing.FirstOrDefault(pa =>
                pa.EnrollmentId == avg.EnrollmentId &&
                pa.PeriodId     == avg.PeriodId);

            if (ex is null)
                await _context.PeriodAverages.AddAsync(avg, ct);
            else
            {
                ex.AvgScore     = avg.AvgScore;
                ex.MaxScore     = avg.MaxScore;
                ex.CalculatedAt = avg.CalculatedAt;
                ex.UpdatedAt    = DateTime.UtcNow;
            }
        }
    }
}



