using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class StudentExamAttemptRepository
    : Repository<StudentExamAttempt>, IStudentExamAttemptRepository
{
    public StudentExamAttemptRepository(AppDbContext context) : base(context) { }


    public async Task<StudentExamAttempt?> GetByEnrollmentAndExamAsync(
        int enrollmentId,
        int examId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .FirstOrDefaultAsync(a =>
                a.EnrollmentId == enrollmentId &&
                a.ExamId       == examId, ct);

    public async Task<bool> HasAttemptedAsync(
        int enrollmentId,
        int examId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .AnyAsync(a =>
                a.EnrollmentId == enrollmentId &&
                a.ExamId       == examId, ct);

    public async Task<StudentExamAttempt?> GetActiveAttemptAsync(
        int enrollmentId,
        int examId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .FirstOrDefaultAsync(a =>
                a.EnrollmentId == enrollmentId &&
                a.ExamId       == examId       &&
                a.SubmittedAt  == null, ct);


    public async Task<IReadOnlyList<StudentExamAttempt>> GetByExamIdAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .Where(a => a.ExamId == examId)
            .Include(a => a.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(a => a.Score)
            .ToListAsync(ct);

    public async Task<int> GetAttemptCountAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .CountAsync(a => a.ExamId == examId, ct);


    public async Task<IReadOnlyList<StudentExamAttempt>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .Where(a => a.EnrollmentId == enrollmentId)
            .Include(a => a.Exam)
                .ThenInclude(e => e.ClassSubjectTeacher)
                    .ThenInclude(cst => cst.Subject)
            .OrderByDescending(a => a.StartedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<StudentExamAttempt>> GetUngradedAsync(
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .Where(a => !a.IsGraded && a.SubmittedAt != null)
            .Include(a => a.Exam)
            .Include(a => a.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentExamAttempt>> GetUngradedByExamAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .Where(a =>
                a.ExamId      == examId &&
                !a.IsGraded            &&
                a.SubmittedAt != null)
            .Include(a => a.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(a => a.SubmittedAt)
            .ToListAsync(ct);


    public async Task<StudentExamAttempt?> GetWithAnswersAsync(
        int attemptId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
                    .ThenInclude(q => q.Options
                        .OrderBy(o => o.DisplayOrder))
            .Include(a => a.Exam)
            .FirstOrDefaultAsync(a => a.Id == attemptId, ct);


    public async Task<decimal> GetAverageScoreAsync(
        int examId,
        CancellationToken ct = default)
    {
        var result = await _context.StudentExamAttempts
            .Where(a =>
                a.ExamId   == examId &&
                a.IsGraded           &&
                a.Score.HasValue)
            .Select(a => (decimal?)a.Score!.Value)
            .AverageAsync(ct);

        return result ?? 0m;
    }

    public async Task<IReadOnlyList<StudentExamAttempt>> GetTopScorersAsync(
        int examId,
        int count,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .Where(a =>
                a.ExamId   == examId &&
                a.IsGraded           &&
                a.Score.HasValue)
            .Include(a => a.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(a => a.Score)
            .Take(count)
            .ToListAsync(ct);
}



