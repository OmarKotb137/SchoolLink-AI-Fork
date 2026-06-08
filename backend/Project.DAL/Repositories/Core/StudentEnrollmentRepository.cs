using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Core;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Core;

public class StudentEnrollmentRepository : Repository<StudentEnrollment>, IStudentEnrollmentRepository
{
    public StudentEnrollmentRepository(AppDbContext context) : base(context) { }


    public async Task<StudentEnrollment?> GetActiveByStudentAndYearAsync(
        int studentId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .FirstOrDefaultAsync(e =>
                e.StudentId == studentId &&
                e.AcademicYearId == academicYearId &&
                e.LeftAt == null, ct);

    public async Task<IReadOnlyList<StudentEnrollment>> GetByClassAndYearAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Where(e => e.ClassId == classId && e.AcademicYearId == academicYearId)
            .Include(e => e.Student)
            .OrderBy(e => e.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentEnrollment>> GetActiveByClassAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Where(e =>
                e.ClassId == classId &&
                e.AcademicYearId == academicYearId &&
                e.LeftAt == null)
            .Include(e => e.Student)
            .OrderBy(e => e.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentEnrollment>> GetActiveByGradeLevelAndYearWithDetailsAsync(
        int gradeLevelId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Where(e =>
                e.AcademicYearId == academicYearId &&
                e.LeftAt == null &&
                !e.IsDeleted &&
                e.Class.GradeLevelId == gradeLevelId &&
                !e.Class.IsDeleted)
            .Include(e => e.Student)
            .Include(e => e.Class)
                .ThenInclude(c => c.GradeLevel)
            .Include(e => e.AcademicYear)
            .OrderBy(e => e.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentEnrollment>> GetHistoryByStudentAsync(
        int studentId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Class)
                .ThenInclude(c => c.GradeLevel)
            .Include(e => e.AcademicYear)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentEnrollment>> GetTransfersHistoryAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Where(e => e.AcademicYearId == academicYearId && e.LeftAt != null && e.TransferReason != null)
            .Include(e => e.Student)
            .Include(e => e.Class)
            .OrderByDescending(e => e.LeftAt)
            .Take(50) // Limit to 50 recent transfers
            .ToListAsync(ct);


    public async Task<bool> IsEnrolledAsync(
        int studentId,
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .AnyAsync(e =>
                e.StudentId == studentId &&
                e.ClassId == classId &&
                e.AcademicYearId == academicYearId &&
                e.LeftAt == null, ct);

    public async Task<bool> HasActiveEnrollmentAsync(
        int studentId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .AnyAsync(e =>
                e.StudentId == studentId &&
                e.AcademicYearId == academicYearId &&
                e.LeftAt == null, ct);


    public async Task<StudentEnrollment?> GetWithStudentAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Include(e => e.Student)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

    public async Task<StudentEnrollment?> GetByIdWithDetailsAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Include(e => e.Student)
            .Include(e => e.Class)
                .ThenInclude(c => c.GradeLevel)
            .Include(e => e.AcademicYear)
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, ct);

    public async Task<IReadOnlyList<StudentEnrollment>> GetByIdsWithDetailsAsync(
        IEnumerable<int> enrollmentIds,
        CancellationToken ct = default)
    {
        var ids = enrollmentIds.Distinct().ToList();
        if (ids.Count == 0)
            return Array.Empty<StudentEnrollment>();

        return await _context.StudentEnrollments
            .Where(e => ids.Contains(e.Id))
            .Include(e => e.Student)
            .Include(e => e.Class)
                .ThenInclude(c => c.GradeLevel)
            .Include(e => e.AcademicYear)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<StudentEnrollment>> GetByClassWithStudentAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .Where(e =>
                e.ClassId == classId &&
                e.AcademicYearId == academicYearId &&
                e.LeftAt == null)
            .Include(e => e.Student)
            .OrderBy(e => e.Student.FullName)
            .ToListAsync(ct);


    public async Task<int> GetActiveCountByClassAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentEnrollments
            .CountAsync(e =>
                e.ClassId == classId &&
                e.AcademicYearId == academicYearId &&
                e.LeftAt == null, ct);
}



