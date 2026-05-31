using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class EvaluationPeriodRepository : Repository<EvaluationPeriod>, IEvaluationPeriodRepository
{
    public EvaluationPeriodRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<EvaluationPeriod>> GetByAcademicYearAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationPeriods
            .Where(p => p.AcademicYearId == academicYearId)
            .OrderBy(p => p.OrderNum)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvaluationPeriod>> GetWeeksByYearAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationPeriods
            .Where(p =>
                p.AcademicYearId == academicYearId &&
                p.PeriodType == PeriodType.Weekly)
            .OrderBy(p => p.OrderNum)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvaluationPeriod>> GetByTypeAndYearAsync(
        int academicYearId,
        PeriodType periodType,
        CancellationToken ct = default)
        => await _context.EvaluationPeriods
            .Where(p =>
                p.AcademicYearId == academicYearId &&
                p.PeriodType == periodType)
            .OrderBy(p => p.OrderNum)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvaluationPeriod>> GetOrderedByYearAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationPeriods
            .Where(p => p.AcademicYearId == academicYearId)
            .OrderBy(p => p.OrderNum)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<EvaluationPeriod>> GetByMonthNameAsync(
        int academicYearId,
        string monthName,
        CancellationToken ct = default)
        => await _context.EvaluationPeriods
            .Where(p =>
                p.AcademicYearId == academicYearId &&
                p.MonthName == monthName &&
                p.PeriodType == PeriodType.Weekly)
            .OrderBy(p => p.OrderNum)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<string>> GetDistinctMonthNamesAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationPeriods
            .Where(p =>
                p.AcademicYearId == academicYearId &&
                p.MonthName != null &&
                p.PeriodType == PeriodType.Weekly)
            .Select(p => p.MonthName!)
            .Distinct()
            .ToListAsync(ct);


    public async Task<EvaluationPeriod?> GetCurrentWeekAsync(
        int academicYearId,
        CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return await _context.EvaluationPeriods
            .Where(p =>
                p.AcademicYearId == academicYearId &&
                p.PeriodType == PeriodType.Weekly &&
                p.StartDate != null &&
                p.EndDate != null &&
                p.StartDate <= today &&
                p.EndDate >= today)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<EvaluationPeriod?> GetByDateAsync(
        int academicYearId,
        DateOnly date,
        CancellationToken ct = default)
        => await _context.EvaluationPeriods
            .Where(p =>
                p.AcademicYearId == academicYearId &&
                p.StartDate != null &&
                p.EndDate != null &&
                p.StartDate <= date &&
                p.EndDate >= date)
            .FirstOrDefaultAsync(ct);
}



