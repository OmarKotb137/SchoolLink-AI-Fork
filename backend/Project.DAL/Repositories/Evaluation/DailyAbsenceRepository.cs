using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class DailyAbsenceRepository : Repository<DailyAbsence>, IDailyAbsenceRepository
{
    public DailyAbsenceRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<DailyAbsence>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .Where(da => da.EnrollmentId == enrollmentId)
            .OrderByDescending(da => da.AbsenceDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DailyAbsence>> GetByEnrollmentAndDateRangeAsync(
        int enrollmentId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .Where(da =>
                da.EnrollmentId == enrollmentId &&
                da.AbsenceDate  >= from         &&
                da.AbsenceDate  <= to)
            .OrderBy(da => da.AbsenceDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DailyAbsence>> GetByEnrollmentAndMonthAsync(
        int enrollmentId,
        int month,
        int year,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .Where(da =>
                da.EnrollmentId        == enrollmentId &&
                da.AbsenceDate.Month   == month        &&
                da.AbsenceDate.Year    == year)
            .OrderBy(da => da.AbsenceDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DateOnly>> GetAbsenceDatesAsync(
        int enrollmentId,
        int? classSubjectTeacherId = null,
        CancellationToken ct = default)
    {
        var query = _context.DailyAbsences
            .Where(da => da.EnrollmentId == enrollmentId && da.IsAbsent);

        if (classSubjectTeacherId.HasValue)
            query = query.Where(da => da.ClassSubjectTeacherId == classSubjectTeacherId);

        return await query
            .Select(da => da.AbsenceDate)
            .OrderBy(d => d)
            .ToListAsync(ct);
    }


    public async Task<IReadOnlyList<DailyAbsence>> GetByClassSubjectTeacherAndDateAsync(
        int classSubjectTeacherId,
        DateOnly date,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .Where(da =>
                da.ClassSubjectTeacherId == classSubjectTeacherId &&
                da.AbsenceDate           == date)
            .Include(da => da.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(da => da.Enrollment.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DailyAbsence>> GetByClassSubjectTeacherAndDateRangeAsync(
        int classSubjectTeacherId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .Where(da =>
                da.ClassSubjectTeacherId == classSubjectTeacherId &&
                da.AbsenceDate           >= from                  &&
                da.AbsenceDate           <= to)
            .Include(da => da.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(da => da.AbsenceDate)
            .ThenBy(da => da.Enrollment.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DailyAbsence>> GetAbsentStudentsByDateAsync(
        int classSubjectTeacherId,
        DateOnly date,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .Where(da =>
                da.ClassSubjectTeacherId == classSubjectTeacherId &&
                da.AbsenceDate           == date                  &&
                da.IsAbsent)
            .Include(da => da.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(da => da.Enrollment.Student.FullName)
            .ToListAsync(ct);


    public async Task<int> GetAbsenceCountAsync(
        int enrollmentId,
        int? classSubjectTeacherId = null,
        DateOnly? from             = null,
        DateOnly? to               = null,
        CancellationToken ct       = default)
    {
        var query = _context.DailyAbsences
            .Where(da => da.EnrollmentId == enrollmentId && da.IsAbsent);

        if (classSubjectTeacherId.HasValue)
            query = query.Where(da => da.ClassSubjectTeacherId == classSubjectTeacherId);

        if (from.HasValue)
            query = query.Where(da => da.AbsenceDate >= from.Value);

        if (to.HasValue)
            query = query.Where(da => da.AbsenceDate <= to.Value);

        return await query.CountAsync(ct);
    }

    public async Task<int> GetAbsenceCountByMonthAsync(
        int enrollmentId,
        int month,
        int year,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .CountAsync(da =>
                da.EnrollmentId      == enrollmentId &&
                da.IsAbsent                          &&
                da.AbsenceDate.Month == month        &&
                da.AbsenceDate.Year  == year, ct);


    public async Task<bool> IsAbsentAsync(
        int enrollmentId,
        DateOnly date,
        int? classSubjectTeacherId = null,
        CancellationToken ct       = default)
    {
        var query = _context.DailyAbsences
            .Where(da =>
                da.EnrollmentId == enrollmentId &&
                da.AbsenceDate  == date         &&
                da.IsAbsent);

        if (classSubjectTeacherId.HasValue)
            query = query.Where(da => da.ClassSubjectTeacherId == classSubjectTeacherId);

        return await query.AnyAsync(ct);
    }


    public async Task<IReadOnlyList<int>> GetEnrollmentsWithAbsenceExceedingAsync(
        int classId,
        int academicYearId,
        int threshold,
        CancellationToken ct = default)
        => await _context.DailyAbsences
            .Where(da =>
                da.IsAbsent                               &&
                da.Enrollment.ClassId         == classId  &&
                da.Enrollment.AcademicYearId  == academicYearId &&
                da.Enrollment.LeftAt          == null)
            .GroupBy(da => da.EnrollmentId)
            .Where(g => g.Count() > threshold)
            .Select(g => g.Key)
            .ToListAsync(ct);


    public async Task BulkUpsertAsync(
        IEnumerable<DailyAbsence> absences,
        CancellationToken ct = default)
    {
        var list = absences.ToList();
        if (!list.Any()) return;

        var enrollmentIds = list.Select(da => da.EnrollmentId).Distinct().ToList();
        var dates         = list.Select(da => da.AbsenceDate).Distinct().ToList();

        var existing = await _context.DailyAbsences
            .Where(da =>
                enrollmentIds.Contains(da.EnrollmentId) &&
                dates.Contains(da.AbsenceDate))
            .ToListAsync(ct);

        foreach (var absence in list)
        {
            var ex = existing.FirstOrDefault(da =>
                da.EnrollmentId          == absence.EnrollmentId          &&
                da.AbsenceDate           == absence.AbsenceDate           &&
                da.ClassSubjectTeacherId == absence.ClassSubjectTeacherId);

            if (ex is null)
                await _context.DailyAbsences.AddAsync(absence, ct);
            else
            {
                ex.IsAbsent     = absence.IsAbsent;
                ex.Reason       = absence.Reason;
                ex.RecordedById = absence.RecordedById;
                ex.UpdatedAt    = DateTime.UtcNow;
            }
        }
    }
}



