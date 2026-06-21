using Microsoft.EntityFrameworkCore;
using Project.DAL.Context;
using Project.DAL.Interfaces.Repositories.Timetable;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.DAL.Repositories.Timetable;

public class TimetableSlotRepository : Repository<TimetableSlot>, ITimetableSlotRepository
{
    public TimetableSlotRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<TimetableSlot>> GetByTimetableIdAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.TimetableId == timetableId)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .Include(s => s.Room)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimetableSlot>> GetNonBreakByTimetableAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.TimetableId == timetableId && !s.IsBreak)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .Include(s => s.Room)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimetableSlot>> GetBreaksByTimetableAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.TimetableId == timetableId && s.IsBreak)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<TimetableSlot>> GetByDayAsync(
        int timetableId,
        SchoolDay day,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.TimetableId == timetableId && s.DayOfWeek == day)
            .OrderBy(s => s.PeriodNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimetableSlot>> GetByDayWithDetailsAsync(
        int timetableId,
        SchoolDay day,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.TimetableId == timetableId && s.DayOfWeek == day)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .Include(s => s.Room)
            .OrderBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<TimetableSlot>> GetByClassSubjectTeacherIdAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.ClassSubjectTeacherId == classSubjectTeacherId)
            .Include(s => s.Room)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<TimetableSlot>> GetTeacherScheduleAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .Where(s =>
                s.ClassSubjectTeacher != null                                       &&
                s.ClassSubjectTeacher.TeacherId      == teacherId                  &&
                s.ClassSubjectTeacher.AcademicYearId == academicYearId             &&
                s.Timetable.IsActive)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.Timetable)
                .ThenInclude(t => t.Class)
            .Include(s => s.Room)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    // ── Slot conflict ──────────────────────────────────────────────────────────

    public async Task<bool> HasConflictAsync(
        int timetableId,
        SchoolDay day,
        int periodNumber,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.TimetableId  == timetableId  &&
                s.DayOfWeek    == day           &&
                s.PeriodNumber == periodNumber, ct);

    public async Task<bool> HasConflictAsync(
        int timetableId,
        SchoolDay day,
        int periodNumber,
        int excludedSlotId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.Id           != excludedSlotId &&
                s.TimetableId  == timetableId    &&
                s.DayOfWeek    == day            &&
                s.PeriodNumber == periodNumber, ct);


    // ── Teacher conflict ───────────────────────────────────────────────────────

    public async Task<bool> HasTeacherConflictAsync(
        int teacherId,
        int academicYearId,
        SchoolDay day,
        int periodNumber,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.ClassSubjectTeacher != null                           &&
                s.ClassSubjectTeacher.TeacherId      == teacherId       &&
                s.ClassSubjectTeacher.AcademicYearId == academicYearId  &&
                s.Timetable.IsActive                                    &&
                s.DayOfWeek    == day                                   &&
                s.PeriodNumber == periodNumber, ct);

    public async Task<bool> HasTeacherConflictAsync(
        int teacherId,
        int academicYearId,
        SchoolDay day,
        int periodNumber,
        int excludedSlotId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.Id          != excludedSlotId                         &&
                s.ClassSubjectTeacher != null                           &&
                s.ClassSubjectTeacher.TeacherId      == teacherId       &&
                s.ClassSubjectTeacher.AcademicYearId == academicYearId  &&
                s.Timetable.IsActive                                    &&
                s.DayOfWeek    == day                                   &&
                s.PeriodNumber == periodNumber, ct);

    /// <inheritdoc/>
    public async Task<string?> GetTeacherConflictClassNameAcrossAllAsync(
        int teacherId,
        int academicYearId,
        SchoolDay day,
        int periodNumber,
        int excludeTimetableId,
        int? excludeSlotId,
        CancellationToken ct = default)
    {
        // نبحث عن أي slot آخر (منشور أو مسودة) محجوز فيه نفس المعلم في نفس اليوم/الحصة.
        var conflict = await _context.TimetableSlots
            .AsNoTracking()
            .Where(s =>
                s.ClassSubjectTeacher != null                           &&
                s.ClassSubjectTeacher.TeacherId      == teacherId       &&
                s.ClassSubjectTeacher.AcademicYearId == academicYearId  &&
                s.DayOfWeek    == day                                   &&
                s.PeriodNumber == periodNumber                          &&
                s.TimetableId  != excludeTimetableId                    &&
                (excludeSlotId == null || s.Id != excludeSlotId.Value)  &&
                !s.IsBreak                                               &&
                !s.IsDeleted && !s.Timetable.IsDeleted)
            .Select(s => new
            {
                ClassName     = s.Timetable.Class.Name,
                SubjectName   = s.ClassSubjectTeacher!.Subject.Name,
                s.Timetable.IsActive
            })
            .FirstOrDefaultAsync(ct);

        if (conflict is null) return null;

        var status = conflict.IsActive ? "منشور" : "مسودة";
        return $"المعلم يدرّس «{conflict.SubjectName}» للفصل «{conflict.ClassName}» ({status}) في نفس اليوم والحصة";
    }


    // ── Room conflict ──────────────────────────────────────────────────────────

    public async Task<bool> HasRoomConflictAsync(
        int roomId,
        SchoolDay day,
        int periodNumber,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.RoomId       == roomId        &&
                s.DayOfWeek    == day           &&
                s.PeriodNumber == periodNumber, ct);

    public async Task<bool> HasRoomConflictAsync(
        int roomId,
        SchoolDay day,
        int periodNumber,
        int excludedSlotId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.Id           != excludedSlotId &&
                s.RoomId       == roomId         &&
                s.DayOfWeek    == day            &&
                s.PeriodNumber == periodNumber, ct);

    public async Task<bool> HasRoomConflictAgainstActiveTimetablesAsync(
        int roomId,
        SchoolDay day,
        int periodNumber,
        int timetableId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AsNoTracking()
            .AnyAsync(s =>
                s.RoomId       == roomId         &&
                s.DayOfWeek    == day            &&
                s.PeriodNumber == periodNumber   &&
                s.TimetableId  != timetableId    &&
                s.Timetable.IsActive, ct);

    /// <inheritdoc/>
    public async Task<RoomConflictInfo?> GetRoomConflictAcrossAllAsync(
        int roomId,
        SchoolDay day,
        int periodNumber,
        int excludeTimetableId,
        int? excludeSlotId,
        CancellationToken ct = default)
    {
        // نبحث عن أي slot آخر (منشور أو مسودة) محجوز في نفس القاعة/اليوم/الحصة.
        var conflict = await _context.TimetableSlots
            .AsNoTracking()
            .Where(s =>
                s.RoomId       == roomId              &&
                s.DayOfWeek    == day                 &&
                s.PeriodNumber == periodNumber        &&
                s.TimetableId  != excludeTimetableId  &&
                (excludeSlotId == null || s.Id != excludeSlotId.Value) &&
                !s.IsDeleted && !s.Timetable.IsDeleted)
            .Select(s => new
            {
                s.Timetable.Class.Name,
                s.Timetable.IsActive,
                SubjectName = s.ClassSubjectTeacher != null
                    ? s.ClassSubjectTeacher.Subject.Name
                    : null,
                TeacherName = s.ClassSubjectTeacher != null && s.ClassSubjectTeacher.Teacher != null
                    ? s.ClassSubjectTeacher.Teacher.FullName
                    : null
            })
            .FirstOrDefaultAsync(ct);

        if (conflict is null) return null;

        return new RoomConflictInfo
        {
            ClassName    = conflict.Name,
            SubjectName  = conflict.SubjectName,
            TeacherName  = conflict.TeacherName,
            IsOtherDraft = !conflict.IsActive
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(int RoomId, SchoolDay DayOfWeek, int PeriodNumber)>> GetRoomConflictsAgainstActiveAsync(
        IEnumerable<(int RoomId, SchoolDay DayOfWeek, int PeriodNumber)> candidates,
        int excludeTimetableId,
        CancellationToken ct = default)
    {
        var list = candidates.ToList();
        if (list.Count == 0)
            return Array.Empty<(int, SchoolDay, int)>();

        // تنفيذ عبر query واحدة بدل N query منفصلة (إصلاح N+1 في الـ validation).
        var conflicts = await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.Timetable.IsActive && s.TimetableId != excludeTimetableId && s.RoomId != null)
            .Select(s => new { s.RoomId!.Value, s.DayOfWeek, s.PeriodNumber })
            .ToListAsync(ct);

        var conflictSet = conflicts
            .Select(c => (c.Value, c.DayOfWeek, c.PeriodNumber))
            .ToHashSet();

        return list.Where(c => conflictSet.Contains((c.RoomId, c.DayOfWeek, c.PeriodNumber))).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(int TeacherId, SchoolDay DayOfWeek, int PeriodNumber)>> GetTeacherConflictsAgainstActiveAsync(
        IEnumerable<(int TeacherId, SchoolDay DayOfWeek, int PeriodNumber)> candidates,
        int excludeTimetableId,
        CancellationToken ct = default)
    {
        var list = candidates.ToList();
        if (list.Count == 0)
            return Array.Empty<(int, SchoolDay, int)>();

        var conflicts = await _context.TimetableSlots
            .AsNoTracking()
            .Where(s => s.Timetable.IsActive
                     && s.TimetableId != excludeTimetableId
                     && s.ClassSubjectTeacher != null
                     && !s.IsBreak)
            .Select(s => new { TeacherId = s.ClassSubjectTeacher!.TeacherId, s.DayOfWeek, s.PeriodNumber })
            .ToListAsync(ct);

        var conflictSet = conflicts
            .Select(c => (c.TeacherId, c.DayOfWeek, c.PeriodNumber))
            .ToHashSet();

        return list.Where(c => conflictSet.Contains((c.TeacherId, c.DayOfWeek, c.PeriodNumber))).ToList();
    }


    // ── Bulk ──────────────────────────────────────────────────────────────────

    public async Task BulkReplaceAsync(
        int timetableId,
        IEnumerable<TimetableSlot> slots,
        CancellationToken ct = default)
    {
        var existing = await _context.TimetableSlots
            .Where(s => s.TimetableId == timetableId)
            .ToListAsync(ct);

        foreach (var slot in existing)
            SoftDelete(slot);

        await _context.TimetableSlots.AddRangeAsync(slots, ct);
    }


    // ── Details ───────────────────────────────────────────────────────────────

    public async Task<TimetableSlot?> GetByIdWithDetailsAsync(
        int slotId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == slotId, ct);
}
