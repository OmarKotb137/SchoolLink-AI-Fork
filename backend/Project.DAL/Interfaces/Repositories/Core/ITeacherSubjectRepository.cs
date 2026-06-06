using Project.DAL.Interfaces.Repositories;
using Project.Domain.Entities;

namespace Project.DAL.Interfaces.Repositories.Core;

public interface ITeacherSubjectRepository : IRepository<TeacherSubject>
{
    Task<IReadOnlyList<TeacherSubject>> GetByTeacherAsync(int teacherId, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherSubject>> GetByTeacherIdsAsync(IEnumerable<int> teacherIds, CancellationToken ct = default);
    Task<IReadOnlyList<TeacherSubject>> GetBySubjectAsync(int subjectId, CancellationToken ct = default);
    Task<bool> ExistsActiveAsync(int teacherId, int subjectId, CancellationToken ct = default);
}
