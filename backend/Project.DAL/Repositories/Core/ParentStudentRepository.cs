using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class ParentStudentRepository : Repository<ParentStudent>, IParentStudentRepository
{
    public ParentStudentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ParentStudent>> GetByParentIdAsync(
        int parentId,
        CancellationToken ct = default)
        => await _context.ParentStudents
            .Where(ps => ps.ParentId == parentId)
            .Include(ps => ps.Student)
            .OrderBy(ps => ps.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ParentStudent>> GetByStudentIdAsync(
        int studentId,
        CancellationToken ct = default)
        => await _context.ParentStudents
            .Where(ps => ps.StudentId == studentId)
            .Include(ps => ps.Parent)
            .OrderBy(ps => ps.Relationship)
            .ToListAsync(ct);

    public async Task<ParentStudent?> GetByParentAndStudentAsync(
        int parentId,
        int studentId,
        CancellationToken ct = default)
        => await _context.ParentStudents
            .Include(ps => ps.Parent)
            .Include(ps => ps.Student)
            .FirstOrDefaultAsync(ps =>
                ps.ParentId == parentId &&
                ps.StudentId == studentId, ct);

    public async Task<bool> ExistsByParentAndStudentAsync(
        int parentId,
        int studentId,
        CancellationToken ct = default)
        => await _context.ParentStudents
            .AnyAsync(ps =>
                ps.ParentId == parentId &&
                ps.StudentId == studentId, ct);


    public async Task<IReadOnlyList<ParentStudent>> GetWithStudentDetailsByParentAsync(
        int parentId,
        CancellationToken ct = default)
        => await _context.ParentStudents
            .Where(ps => ps.ParentId == parentId)
            .Include(ps => ps.Student)
                .ThenInclude(s => s.Enrollments
                    .Where(e => e.LeftAt == null))
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.GradeLevel)
            .OrderBy(ps => ps.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ParentStudent>> GetWithParentDetailsByStudentAsync(
        int studentId,
        CancellationToken ct = default)
        => await _context.ParentStudents
            .Where(ps => ps.StudentId == studentId)
            .Include(ps => ps.Parent)
            .OrderBy(ps => ps.Relationship)
            .ToListAsync(ct);
}



