using Project.Domain.Entities;
using Project.Domain.Enums;
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
    Task<bool> HasConflictAsync(int timetableId, SchoolDay day, int periodNumber, int excludedSlotId, CancellationToken ct = default);
    Task<bool> HasTeacherConflictAsync(int teacherId, int academicYearId, SchoolDay day, int periodNumber, CancellationToken ct = default);
    Task<bool> HasTeacherConflictAsync(int teacherId, int academicYearId, SchoolDay day, int periodNumber, int excludedSlotId, CancellationToken ct = default);

    Task BulkReplaceAsync(int timetableId, IEnumerable<TimetableSlot> slots, CancellationToken ct = default);

    /// <summary>
    /// يجيب TimetableSlot بـ ClassSubjectTeacher → Subject + Teacher محملين.
    /// يُستخدم في AddTimetableSlotAsync و UpdateTimetableSlotAsync بعد الـ save
    /// لإرجاع SubjectName و TeacherName في الـ DTO.
    /// </summary>
    Task<TimetableSlot?> GetByIdWithDetailsAsync(int slotId, CancellationToken ct = default);
}

