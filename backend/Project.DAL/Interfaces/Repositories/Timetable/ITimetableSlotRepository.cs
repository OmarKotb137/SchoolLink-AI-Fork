using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Timetable;

public interface ITimetableSlotRepository : IRepository<TimetableSlot>
{
    Task<IReadOnlyList<TimetableSlot>> GetByTimetableIdAsync(int timetableId, CancellationToken ct = default);
    Task<IReadOnlyList<TimetableSlot>> GetNonBreakByTimetableAsync(int timetableId, CancellationToken ct = default);
    Task<IReadOnlyList<TimetableSlot>> GetBreaksByTimetableAsync(int timetableId, CancellationToken ct = default);

    Task<IReadOnlyList<TimetableSlot>> GetByDayAsync(int timetableId, SchoolDay day, CancellationToken ct = default);
    Task<IReadOnlyList<TimetableSlot>> GetByDayWithDetailsAsync(int timetableId, SchoolDay day, CancellationToken ct = default);

    Task<IReadOnlyList<TimetableSlot>> GetByClassSubjectTeacherIdAsync(int classSubjectTeacherId, CancellationToken ct = default);

    Task<IReadOnlyList<TimetableSlot>> GetTeacherScheduleAsync(int teacherId, int academicYearId, CancellationToken ct = default);

    Task<bool> HasConflictAsync(int timetableId, SchoolDay day, int periodNumber, CancellationToken ct = default);

    Task BulkReplaceAsync(int timetableId, IEnumerable<TimetableSlot> slots, CancellationToken ct = default);
}



