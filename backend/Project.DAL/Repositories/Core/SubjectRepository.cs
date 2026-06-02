using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class SubjectRepository : Repository<Subject>, ISubjectRepository
{
    public SubjectRepository(AppDbContext context) : base(context) { }

    public async Task<Subject?> GetByCodeAsync(
        string code,
        CancellationToken ct = default)
        => await _context.Subjects
            .FirstOrDefaultAsync(s => s.Code == code.ToUpper(), ct);

    public async Task<Subject?> GetByNameAsync(
        string name,
        CancellationToken ct = default)
        => await _context.Subjects
            .FirstOrDefaultAsync(s => s.Name == name, ct);

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken ct = default)
        => await _context.Subjects
            .AnyAsync(s => s.Name == name, ct);

    public async Task<bool> ExistsByCodeAsync(
        string code,
        CancellationToken ct = default)
        => await _context.Subjects
            .AnyAsync(s => s.Code == code.ToUpper(), ct);
}



