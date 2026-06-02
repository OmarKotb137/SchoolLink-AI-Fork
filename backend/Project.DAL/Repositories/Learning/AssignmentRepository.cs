using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class AssignmentRepository : Repository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<Assignment>> GetByClassSubjectTeacherIdAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.Assignments
            .Where(a => a.ClassSubjectTeacherId == classSubjectTeacherId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Assignment>> GetByCategoryAsync(
        int classSubjectTeacherId,
        EvaluationCategory category,
        CancellationToken ct = default)
        => await _context.Assignments
            .Where(a =>
                a.ClassSubjectTeacherId == classSubjectTeacherId &&
                a.Category              == category)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Assignment>> GetAIGeneratedAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.Assignments
            .Where(a =>
                a.ClassSubjectTeacherId == classSubjectTeacherId &&
                a.IsAIGenerated)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Assignment>> GetByClassIdAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Assignments
            .Where(a =>
                a.ClassSubjectTeacher.ClassId         == classId        &&
                a.ClassSubjectTeacher.AcademicYearId  == academicYearId)
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Assignment>> GetUpcomingByClassAsync(
        int classId,
        int days,
        CancellationToken ct = default)
    {
        var now    = DateTime.UtcNow;
        var cutoff = now.AddDays(days);

        return await _context.Assignments
            .Where(a =>
                a.ClassSubjectTeacher.ClassId == classId &&
                a.DueDate.HasValue                       &&
                a.DueDate >= now                         &&
                a.DueDate <= cutoff)
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderBy(a => a.DueDate)
            .ToListAsync(ct);
    }


    public async Task<IReadOnlyList<Assignment>> GetPendingForEnrollmentAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.Assignments
            .Where(a =>
                _context.StudentEnrollments
                    .Where(e => e.Id == enrollmentId && e.LeftAt == null)
                    .Any(e => a.ClassSubjectTeacher.ClassId == e.ClassId) &&
                !_context.StudentAssignmentSubmissions
                    .Any(sub => sub.EnrollmentId == enrollmentId && sub.AssignmentId == a.Id) &&
                (a.DueDate == null || a.DueDate >= DateTime.UtcNow))
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderBy(a => a.DueDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Assignment>> GetOverdueForEnrollmentAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.Assignments
            .Where(a =>
                _context.StudentEnrollments
                    .Where(e => e.Id == enrollmentId && e.LeftAt == null)
                    .Any(e => a.ClassSubjectTeacher.ClassId == e.ClassId) &&
                !_context.StudentAssignmentSubmissions
                    .Any(sub => sub.EnrollmentId == enrollmentId && sub.AssignmentId == a.Id) &&
                a.DueDate.HasValue &&
                a.DueDate < DateTime.UtcNow)
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderByDescending(a => a.DueDate)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Assignment>> GetByDueDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
        => await _context.Assignments
            .Where(a =>
                a.DueDate.HasValue &&
                a.DueDate >= from  &&
                a.DueDate <= to)
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderBy(a => a.DueDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Assignment>> GetOverdueAsync(
        int? classSubjectTeacherId = null,
        CancellationToken ct       = default)
    {
        var query = _context.Assignments
            .Where(a => a.DueDate.HasValue && a.DueDate < DateTime.UtcNow);

        if (classSubjectTeacherId.HasValue)
            query = query.Where(a => a.ClassSubjectTeacherId == classSubjectTeacherId);

        return await query
            .Include(a => a.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderByDescending(a => a.DueDate)
            .ToListAsync(ct);
    }


    public async Task<Assignment?> GetWithQuestionsAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.Assignments
            .Include(a => a.Questions
                .OrderBy(q => q.DisplayOrder))
                .ThenInclude(q => q.Options
                    .OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(a => a.Id == assignmentId, ct);
}



