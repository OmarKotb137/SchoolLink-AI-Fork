using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class StudentAssignmentSubmissionRepository
    : Repository<StudentAssignmentSubmission>, IStudentAssignmentSubmissionRepository
{
    public StudentAssignmentSubmissionRepository(AppDbContext context) : base(context) { }


    public async Task<StudentAssignmentSubmission?> GetByEnrollmentAndAssignmentAsync(
        int enrollmentId,
        int assignmentId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .FirstOrDefaultAsync(sub =>
                sub.EnrollmentId  == enrollmentId &&
                sub.AssignmentId  == assignmentId, ct);

    public async Task<bool> HasSubmittedAsync(
        int enrollmentId,
        int assignmentId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .AnyAsync(sub =>
                sub.EnrollmentId == enrollmentId &&
                sub.AssignmentId == assignmentId, ct);


    public async Task<IReadOnlyList<StudentAssignmentSubmission>> GetByAssignmentIdAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .Where(sub => sub.AssignmentId == assignmentId)
            .Include(sub => sub.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(sub => sub.SubmittedAt)
            .ToListAsync(ct);

    public async Task<int> GetSubmissionCountAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .CountAsync(sub => sub.AssignmentId == assignmentId, ct);


    public async Task<IReadOnlyList<StudentAssignmentSubmission>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .Where(sub => sub.EnrollmentId == enrollmentId)
            .Include(sub => sub.Assignment)
                .ThenInclude(a => a.ClassSubjectTeacher)
                    .ThenInclude(cst => cst.Subject)
            .OrderByDescending(sub => sub.SubmittedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<StudentAssignmentSubmission>> GetUngradedAsync(
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .Where(sub => !sub.IsGraded)
            .Include(sub => sub.Assignment)
            .Include(sub => sub.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(sub => sub.SubmittedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentAssignmentSubmission>> GetUngradedByAssignmentAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .Where(sub => sub.AssignmentId == assignmentId && !sub.IsGraded)
            .Include(sub => sub.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(sub => sub.SubmittedAt)
            .ToListAsync(ct);


    public async Task<StudentAssignmentSubmission?> GetWithAnswersAsync(
        int submissionId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .Include(sub => sub.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.Options
                        .OrderBy(o => o.DisplayOrder))
            .Include(sub => sub.Assignment)
            .FirstOrDefaultAsync(sub => sub.Id == submissionId, ct);


    public async Task<decimal> GetAverageScoreAsync(
        int assignmentId,
        CancellationToken ct = default)
    {
        var result = await _context.StudentAssignmentSubmissions
            .Where(sub =>
                sub.AssignmentId == assignmentId &&
                sub.IsGraded     &&
                sub.Score.HasValue)
            .Select(sub => (decimal?)sub.Score!.Value)
            .AverageAsync(ct);

        return result ?? 0m;
    }

    public async Task<IReadOnlyList<StudentAssignmentSubmission>> GetPendingByTeacherAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentSubmissions
            .Where(sub =>
                !sub.IsGraded &&
                sub.Assignment.ClassSubjectTeacher.TeacherId      == teacherId      &&
                sub.Assignment.ClassSubjectTeacher.AcademicYearId == academicYearId)
            .Include(sub => sub.Enrollment)
                .ThenInclude(e => e.Student)
            .Include(sub => sub.Assignment)
                .ThenInclude(a => a.ClassSubjectTeacher)
                    .ThenInclude(cst => cst.Subject)
            .OrderBy(sub => sub.SubmittedAt)
            .ToListAsync(ct);
}



