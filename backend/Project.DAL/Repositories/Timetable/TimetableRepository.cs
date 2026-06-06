using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Timetable;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Timetable;

public class TimetableRepository : Repository<Project.Domain.Entities.Timetable>, ITimetableRepository
{
    public TimetableRepository(AppDbContext context) : base(context) { }


    public async Task<Project.Domain.Entities.Timetable?> GetActiveByClassAndYearAsync(
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


    public async Task<IReadOnlyList<Project.Domain.Entities.Timetable>> GetByClassAndYearAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Where(t =>
                t.ClassId        == classId        &&
                t.AcademicYearId == academicYearId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Project.Domain.Entities.Timetable>> GetByClassAndYearWithDetailsAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Where(t =>
                t.ClassId        == classId        &&
                t.AcademicYearId == academicYearId)
            .Include(t => t.Class)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Subject)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Teacher)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.Room)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);


    public async Task<Project.Domain.Entities.Timetable?> GetWithSlotsAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Include(t => t.Slots
                .Where(s => !s.IsBreak))
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


    public async Task<Project.Domain.Entities.Timetable?> GetWithClassAndAllSlotsAsync(
        int timetableId,
        CancellationToken ct = default)
        => await _context.Timetables
            .Include(t => t.Class)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Subject)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.ClassSubjectTeacher)
                    .ThenInclude(cst => cst!.Teacher)
            .Include(t => t.Slots
                .Where(s => !s.IsDeleted))
                .ThenInclude(s => s.Room)
            .FirstOrDefaultAsync(t => t.Id == timetableId, ct);
}


