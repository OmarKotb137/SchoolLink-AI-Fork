using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Timetable;

public interface ITimetableRepository : IRepository<SchoolLink.Domain.Entities.Timetable>
{
    Task<SchoolLink.Domain.Entities.Timetable?> GetActiveByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<bool>       HasActiveTimetableAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<IReadOnlyList<SchoolLink.Domain.Entities.Timetable>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<SchoolLink.Domain.Entities.Timetable?> GetWithSlotsAsync(int timetableId, CancellationToken ct = default);

    Task DeactivateByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
}



