using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Timetable;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Timetable;

public class TimetableRepository : Repository<SchoolLink.Domain.Entities.Timetable>, ITimetableRepository
{
    public TimetableRepository(AppDbContext context) : base(context) { }


    public async Task<SchoolLink.Domain.Entities.Timetable?> GetActiveByClassAndYearAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Timetables
            .FirstOrDefaultAsync(t =>
                t.ClassId        == classId        &&
                t.AcademicYearId == academicYearId &&
                t.IsActive, ct);

    public async Task<bool> HasActiveTimetableAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Timetables
            .AnyAsync(t =>
                t.ClassId        == classId        &&
                t.AcademicYearId == academicYearId &&
                t.IsActive, ct);


    public async Task<IReadOnlyList<SchoolLink.Domain.Entities.Timetable>> GetByClassAndYearAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Where(t =>
                t.ClassId        == classId        &&
                t.AcademicYearId == academicYearId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SchoolLink.Domain.Entities.Timetable>> GetByClassAndYearWithDetailsAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Where(t =>
                t.ClassId        == classId        &&
                t.AcademicYearId == academicYearId)
            .Include(t => t.Class)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.PeriodNumber))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Subject)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Teacher)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);


    public async Task<SchoolLink.Domain.Entities.Timetable?> GetWithSlotsAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Include(t => t.Slots
                .Where(s => !s.IsBreak)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.PeriodNumber))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Subject)
            .Include(t => t.Slots
                .Where(s => !s.IsBreak))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Teacher)
            .FirstOrDefaultAsync(t => t.Id == timetableId, ct);


    public async Task DeactivateByClassAndYearAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Where(t =>
                t.ClassId        == classId        &&
                t.AcademicYearId == academicYearId &&
                t.IsActive)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.IsActive,   false)
                .SetProperty(t => t.UpdatedAt,  DateTime.UtcNow), ct);


    public async Task<SchoolLink.Domain.Entities.Timetable?> GetWithClassAndAllSlotsAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Include(t => t.Class)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.PeriodNumber))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Subject)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Teacher)
            .FirstOrDefaultAsync(t => t.Id == timetableId, ct);
}


