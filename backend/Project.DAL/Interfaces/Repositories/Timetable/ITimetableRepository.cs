using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Timetable;

public interface ITimetableRepository : IRepository<Project.Domain.Entities.Timetable>
{
    Task<Project.Domain.Entities.Timetable?> GetActiveByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<bool>       HasActiveTimetableAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<IReadOnlyList<Project.Domain.Entities.Timetable>> GetByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);

    Task<Project.Domain.Entities.Timetable?> GetWithSlotsAsync(int timetableId, CancellationToken ct = default);

    Task DeactivateByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
}



