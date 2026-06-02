using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class AcademicYearRepository : Repository<AcademicYear>, IAcademicYearRepository
{
    public AcademicYearRepository(AppDbContext context) : base(context) { }

    public async Task<AcademicYear?> GetCurrentAsync(CancellationToken ct = default)
        => await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.IsCurrent, ct);

    public async Task<AcademicYear?> GetByNameAsync(
        string name,
        CancellationToken ct = default)
        => await _context.AcademicYears
            .FirstOrDefaultAsync(y => y.Name == name, ct);

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken ct = default)
        => await _context.AcademicYears
            .AnyAsync(y => y.Name == name, ct);

    public async Task<bool> HasCurrentYearAsync(CancellationToken ct = default)
        => await _context.AcademicYears
            .AnyAsync(y => y.IsCurrent, ct);

    public async Task<IReadOnlyList<AcademicYear>> GetAllOrderedByStartDateAsync(
        CancellationToken ct = default)
        => await _context.AcademicYears
            .OrderByDescending(y => y.StartDate)
            .ToListAsync(ct);
}



