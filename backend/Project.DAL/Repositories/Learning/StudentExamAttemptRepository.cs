using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using Project.Domain.Entities;
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
                    .ThenInclude(s => s.User)
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
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

    public async Task<StudentExamAttempt?> GetWithAnswersForEnrollmentAsync(
        int attemptId,
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.StudentExamAttempts
            .AsSplitQuery()                                     // ✅ يمنع Cartesian Explosion بين Collections
            .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
                    .ThenInclude(q => q.Options
                        .OrderBy(o => o.DisplayOrder))
            .Include(a => a.Exam)
                .ThenInclude(e => e.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.Options.Where(o => !o.IsDeleted))
            .Include(a => a.Exam)
                .ThenInclude(e => e.Subject)
            .Include(a => a.Exam)
                .ThenInclude(e => e.GradeLevel)
            .Include(a => a.Exam)
                .ThenInclude(e => e.ClassSubjectTeacher)
                    .ThenInclude(cst => cst.Subject)
            .FirstOrDefaultAsync(a => a.Id == attemptId && a.EnrollmentId == enrollmentId, ct);


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


    public async Task<IReadOnlyList<StudentExamAttempt>> GetExpiredUnsubmittedAsync(
        CancellationToken ct = default)
    {
        var unsubmittedAttempts = await _context.StudentExamAttempts
            .Where(a => a.SubmittedAt == null)
            .Include(a => a.Exam)
            .ToListAsync(ct);

        var nowUtc = DateTime.UtcNow;

        return unsubmittedAttempts.Where(a =>
        {
            var startedAtUtc = DateTime.SpecifyKind(a.StartedAt, DateTimeKind.Utc);

            // الوقت انتهى عن طريق Duration الامتحان
            // ✅ Grace Period دقيقتين: نديها وقت كافي للـ Frontend يسلّم بنفسه قبل ما الـ Background يتدخل
            //    لو الطالب سلّم بنفسه في آخر ثانية، الـ Background مش هيتصادم معاه
            const int gracePeriodMinutes = 2;

            var isTimeUpByDuration = a.Exam.DurationMinutes.HasValue &&
                startedAtUtc.AddMinutes(a.Exam.DurationMinutes.Value).AddMinutes(gracePeriodMinutes) < nowUtc;

            // أو وقت الامتحان الكلي انتهى — EndTime محفوظ بتوقيت مصر (UTC+3)
            var isTimeUpByEndTime = a.Exam.EndTime.HasValue &&
                a.Exam.EndTime.Value.AddHours(-3).AddMinutes(gracePeriodMinutes) < nowUtc;

            return isTimeUpByDuration || isTimeUpByEndTime;
        }).ToList();
    }
}



