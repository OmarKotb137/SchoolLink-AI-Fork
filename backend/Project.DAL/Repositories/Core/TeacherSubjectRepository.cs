using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;

namespace Project.DAL.Repositories.Core;

public class TeacherSubjectRepository : Repository<TeacherSubject>, ITeacherSubjectRepository
{
    public TeacherSubjectRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<TeacherSubject>> GetByTeacherAsync(int teacherId, CancellationToken ct = default)
        => await _context.Set<TeacherSubject>()
            .Where(x => x.TeacherId == teacherId && !x.IsDeleted)
            .OrderBy(x => x.SubjectId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TeacherSubject>> GetByTeacherIdsAsync(IEnumerable<int> teacherIds, CancellationToken ct = default)
    {
        var ids = teacherIds.Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<TeacherSubject>();

        return await _context.Set<TeacherSubject>()
            .Where(x => ids.Contains(x.TeacherId) && !x.IsDeleted)
            .OrderBy(x => x.TeacherId)
            .ThenBy(x => x.SubjectId)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<TeacherSubject>> GetBySubjectAsync(int subjectId, CancellationToken ct = default)
        => await _context.Set<TeacherSubject>()
            .Where(x => x.SubjectId == subjectId && !x.IsDeleted)
            .OrderBy(x => x.TeacherId)
            .ToListAsync(ct);

    public async Task<bool> ExistsActiveAsync(int teacherId, int subjectId, CancellationToken ct = default)
        => await _context.Set<TeacherSubject>()
            .AnyAsync(x => x.TeacherId == teacherId && x.SubjectId == subjectId && !x.IsDeleted, ct);
}
