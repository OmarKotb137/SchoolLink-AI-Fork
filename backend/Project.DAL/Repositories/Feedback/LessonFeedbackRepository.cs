using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Feedback;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Feedback;

public class LessonFeedbackRepository : Repository<LessonFeedback>, ILessonFeedbackRepository
{
    public LessonFeedbackRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<LessonFeedback>> GetByClassSubjectTeacherIdAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.LessonFeedbacks
            .Where(lf => lf.ClassSubjectTeacherId == classSubjectTeacherId)
            .Include(lf => lf.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(lf => lf.LessonDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LessonFeedback>> GetByLessonDateAsync(
        int classSubjectTeacherId,
        DateOnly lessonDate,
        CancellationToken ct = default)
        => await _context.LessonFeedbacks
            .Where(lf =>
                lf.ClassSubjectTeacherId == classSubjectTeacherId &&
                lf.LessonDate            == lessonDate)
            .Include(lf => lf.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(lf => lf.Enrollment.Student.FullName)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<LessonFeedback>> GetByDateRangeAsync(
        int classSubjectTeacherId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
        => await _context.LessonFeedbacks
            .Where(lf =>
                lf.ClassSubjectTeacherId == classSubjectTeacherId &&
                lf.LessonDate            >= from                  &&
                lf.LessonDate            <= to)
            .Include(lf => lf.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(lf => lf.LessonDate)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<LessonFeedback>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.LessonFeedbacks
            .Where(lf => lf.EnrollmentId == enrollmentId)
            .Include(lf => lf.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .Include(lf => lf.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Teacher)
            .OrderByDescending(lf => lf.LessonDate)
            .ToListAsync(ct);


    public async Task<bool> HasFeedbackAsync(
        int enrollmentId,
        int classSubjectTeacherId,
        DateOnly lessonDate,
        CancellationToken ct = default)
        => await _context.LessonFeedbacks
            .AnyAsync(lf =>
                lf.EnrollmentId          == enrollmentId          &&
                lf.ClassSubjectTeacherId == classSubjectTeacherId &&
                lf.LessonDate            == lessonDate, ct);


    public async Task<decimal> GetAverageRatingAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
    {
        var result = await _context.LessonFeedbacks
            .Where(lf => lf.ClassSubjectTeacherId == classSubjectTeacherId)
            .Select(lf => (decimal?)lf.Rating)
            .AverageAsync(ct);

        return result ?? 0m;
    }

    public async Task<decimal> GetAverageRatingByDateRangeAsync(
        int classSubjectTeacherId,
        DateOnly from,
        DateOnly to,
        CancellationToken ct = default)
    {
        var result = await _context.LessonFeedbacks
            .Where(lf =>
                lf.ClassSubjectTeacherId == classSubjectTeacherId &&
                lf.LessonDate            >= from                  &&
                lf.LessonDate            <= to)
            .Select(lf => (decimal?)lf.Rating)
            .AverageAsync(ct);

        return result ?? 0m;
    }

    public async Task<Dictionary<LessonUnderstanding, int>> GetUnderstandingStatsAsync(
        int classSubjectTeacherId,
        DateOnly lessonDate,
        CancellationToken ct = default)
    {
        var groups = await _context.LessonFeedbacks
            .Where(lf =>
                lf.ClassSubjectTeacherId == classSubjectTeacherId &&
                lf.LessonDate            == lessonDate)
            .GroupBy(lf => lf.Understanding)
            .Select(g => new { Understanding = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return groups.ToDictionary(x => x.Understanding, x => x.Count);
    }

    public async Task<int> GetFeedbackCountByLessonAsync(
        int classSubjectTeacherId,
        DateOnly lessonDate,
        CancellationToken ct = default)
        => await _context.LessonFeedbacks
            .CountAsync(lf =>
                lf.ClassSubjectTeacherId == classSubjectTeacherId &&
                lf.LessonDate            == lessonDate, ct);

    public async Task<decimal> GetOverallTeacherRatingAsync(
        int teacherId,
        int academicYearId,
        CancellationToken ct = default)
    {
        var result = await _context.LessonFeedbacks
            .Where(lf =>
                lf.ClassSubjectTeacher.TeacherId      == teacherId      &&
                lf.ClassSubjectTeacher.AcademicYearId == academicYearId)
            .Select(lf => (decimal?)lf.Rating)
            .AverageAsync(ct);

        return result ?? 0m;
    }
}



