using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class FinalGradeRepository : Repository<FinalGrade>, IFinalGradeRepository
{
    public FinalGradeRepository(AppDbContext context) : base(context) { }


    public async Task<FinalGrade?> GetByEnrollmentIdAsync(
        int enrollmentId,
        AcademicTerm? term = null,
        int? subjectId = null,
        CancellationToken ct = default)
    {
        var query = _context.FinalGrades.Where(fg => fg.EnrollmentId == enrollmentId);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        if (subjectId.HasValue)
            query = query.Where(fg => fg.SubjectId == subjectId.Value);

        return await query.FirstOrDefaultAsync(ct);
    }


    public async Task<IReadOnlyList<FinalGrade>> GetByClassIdAsync(
        int classId,
        AcademicTerm? term = null,
        int? subjectId = null,
        CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        if (subjectId.HasValue)
            query = query.Where(fg => fg.SubjectId == subjectId.Value);

        return await query
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(fg => fg.Total)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinalGrade>> GetPublishedByClassIdAsync(
        int classId,
        AcademicTerm? term = null,
        CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null &&
                fg.IsPublished);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        return await query
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(fg => fg.Total)
            .ToListAsync(ct);
    }


    public async Task<IReadOnlyList<FinalGrade>> GetTopStudentsByClassAsync(
        int classId,
        int count,
        AcademicTerm? term = null,
        CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        return await query
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(fg => fg.Total)
            .Take(count)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FinalGrade>> GetStudentsNeedingSupportAsync(
        int classId,
        decimal threshold,
        AcademicTerm? term = null,
        CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null &&
                fg.Total < threshold);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        return await query
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(fg => fg.Total)
            .ToListAsync(ct);
    }

    public async Task<decimal> GetClassAverageAsync(
        int classId,
        AcademicTerm? term = null,
        CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        var result = await query
            .Select(fg => (decimal?)fg.Total)
            .AverageAsync(ct);

        return result ?? 0m;
    }


    public async Task<IReadOnlyList<FinalGrade>> GetStudentsBelowThresholdAsync(
        int classId,
        decimal threshold,
        AcademicTerm? term = null,
        CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null &&
                fg.Total < threshold);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        return await query
            .Include(fg => fg.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderBy(fg => fg.Total)
            .ToListAsync(ct);
    }


    public async Task UpsertAsync(FinalGrade finalGrade, CancellationToken ct = default)
    {
        var existing = await _context.FinalGrades
            .FirstOrDefaultAsync(fg =>
                fg.EnrollmentId == finalGrade.EnrollmentId &&
                fg.Term == finalGrade.Term &&
                fg.SubjectId == finalGrade.SubjectId, ct);

        if (existing is null)
            await _context.FinalGrades.AddAsync(finalGrade, ct);
        else
        {
            existing.PeriodAvgScore   = finalGrade.PeriodAvgScore;
            existing.Assessment1Score = finalGrade.Assessment1Score;
            existing.Assessment2Score = finalGrade.Assessment2Score;
            existing.WrittenTotal     = finalGrade.WrittenTotal;
            existing.FinalExamScore   = finalGrade.FinalExamScore;
            existing.Total            = finalGrade.Total;
            existing.UpdatedAt        = DateTime.UtcNow;
        }
    }

    public async Task BulkUpsertAsync(
        IEnumerable<FinalGrade> finalGrades,
        CancellationToken ct = default)
    {
        var list = finalGrades.ToList();
        if (!list.Any()) return;

        var keys = list.Select(fg => new { fg.EnrollmentId, fg.Term, fg.SubjectId }).Distinct().ToList();
        var enrollmentIds = keys.Select(k => k.EnrollmentId).Distinct().ToList();

        var existing = await _context.FinalGrades
            .Where(fg => enrollmentIds.Contains(fg.EnrollmentId))
            .ToListAsync(ct);

        foreach (var grade in list)
        {
            var ex = existing.FirstOrDefault(fg =>
                fg.EnrollmentId == grade.EnrollmentId &&
                fg.Term == grade.Term &&
                fg.SubjectId == grade.SubjectId);

            if (ex is null)
                await _context.FinalGrades.AddAsync(grade, ct);
            else
            {
                ex.PeriodAvgScore   = grade.PeriodAvgScore;
                ex.Assessment1Score = grade.Assessment1Score;
                ex.Assessment2Score = grade.Assessment2Score;
                ex.WrittenTotal     = grade.WrittenTotal;
                ex.FinalExamScore   = grade.FinalExamScore;
                ex.Total            = grade.Total;
                ex.UpdatedAt        = DateTime.UtcNow;
            }
        }
    }


    public async Task BulkPublishByClassAsync(int classId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        await query
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(fg => fg.IsPublished, true)
                .SetProperty(fg => fg.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task BulkUnpublishByClassAsync(int classId, AcademicTerm? term = null, CancellationToken ct = default)
    {
        var query = _context.FinalGrades
            .Where(fg =>
                fg.Enrollment.ClassId == classId &&
                fg.Enrollment.LeftAt == null);

        if (term.HasValue)
            query = query.Where(fg => fg.Term == term.Value);

        await query
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(fg => fg.IsPublished, false)
                .SetProperty(fg => fg.UpdatedAt, DateTime.UtcNow), ct);
    }
}
