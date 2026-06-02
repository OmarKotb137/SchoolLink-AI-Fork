using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Timetable;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Timetable;

public class TimetableSlotRepository : Repository<TimetableSlot>, ITimetableSlotRepository
{
    public TimetableSlotRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<TimetableSlot>> GetByTimetableIdAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Where(s => s.TimetableId == timetableId)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimetableSlot>> GetNonBreakByTimetableAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Where(s => s.TimetableId == timetableId && !s.IsBreak)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimetableSlot>> GetBreaksByTimetableAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Where(s => s.TimetableId == timetableId && s.IsBreak)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<TimetableSlot>> GetByDayAsync(
        int timetableId,
        SchoolDay day,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Where(s => s.TimetableId == timetableId && s.DayOfWeek == day)
            .OrderBy(s => s.PeriodNumber)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TimetableSlot>> GetByDayWithDetailsAsync(
        int timetableId,
        SchoolDay day,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Where(s => s.TimetableId == timetableId && s.DayOfWeek == day)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .OrderBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<TimetableSlot>> GetByClassSubjectTeacherIdAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Where(s => s.ClassSubjectTeacherId == classSubjectTeacherId)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<TimetableSlot>> GetTeacherScheduleAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Where(s =>
                s.ClassSubjectTeacher != null                                        &&
                s.ClassSubjectTeacher.TeacherId      == teacherId                   &&
                s.ClassSubjectTeacher.AcademicYearId == academicYearId             &&
                s.Timetable.IsActive)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.Timetable)
                .ThenInclude(t => t.Class)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.PeriodNumber)
            .ToListAsync(ct);


    public async Task<bool> HasConflictAsync(
        int timetableId,
        SchoolDay day,
        int periodNumber,
        CancellationToken ct = default)
        => await _context.TimetableSlots
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
            .AnyAsync(s =>
                s.Id          != excludedSlotId &&
                s.TimetableId == timetableId    &&
                s.DayOfWeek   == day            &&
                s.PeriodNumber == periodNumber, ct);

    public async Task<bool> HasTeacherConflictAsync(
        int teacherId,
        int academicYearId,
        SchoolDay day,
        int periodNumber,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AnyAsync(s =>
                s.ClassSubjectTeacher != null                          &&
                s.ClassSubjectTeacher.TeacherId      == teacherId      &&
                s.ClassSubjectTeacher.AcademicYearId == academicYearId &&
                s.Timetable.IsActive                                   &&
                s.DayOfWeek    == day                                  &&
                s.PeriodNumber == periodNumber, ct);

    public async Task<bool> HasTeacherConflictAsync(
        int teacherId,
        int academicYearId,
        SchoolDay day,
        int periodNumber,
        int excludedSlotId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .AnyAsync(s =>
                s.Id          != excludedSlotId                        &&
                s.ClassSubjectTeacher != null                          &&
                s.ClassSubjectTeacher.TeacherId      == teacherId      &&
                s.ClassSubjectTeacher.AcademicYearId == academicYearId &&
                s.Timetable.IsActive                                   &&
                s.DayOfWeek    == day                                  &&
                s.PeriodNumber == periodNumber, ct);


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


    public async Task<TimetableSlot?> GetByIdWithDetailsAsync(
        int slotId,
        CancellationToken ct = default)
        => await _context.TimetableSlots
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Subject)
            .Include(s => s.ClassSubjectTeacher)
                .ThenInclude(cst => cst!.Teacher)
            .FirstOrDefaultAsync(s => s.Id == slotId, ct);
}
