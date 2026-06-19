using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class PeriodicAssessmentRepository : Repository<PeriodicAssessment>, IPeriodicAssessmentRepository
{
    public PeriodicAssessmentRepository(AppDbContext context) : base(context) { }


    public async Task<PeriodicAssessment?> GetByEnrollmentAndTypeAsync(
        int enrollmentId,
        PeriodicAssessmentType assessmentType,
        AcademicTerm? term = null,
        int? subjectId = null,
        CancellationToken ct = default)
    {
        var query = _context.PeriodicAssessments
            .Where(pa =>
                pa.EnrollmentId == enrollmentId &&
                pa.AssessmentType == assessmentType);

        if (subjectId.HasValue)
            query = query.Where(pa => pa.SubjectId == subjectId.Value);

        if (term.HasValue)
            query = query.Where(pa => pa.Term == term.Value);
        else
            query = query.Where(pa => pa.Term == null);

        return await query.FirstOrDefaultAsync(ct);
    }


    public async Task<IReadOnlyList<PeriodicAssessment>> GetByEnrollmentIdAsync(
        int enrollmentId,
        CancellationToken ct = default)
        => await _context.PeriodicAssessments
            .Where(pa => pa.EnrollmentId == enrollmentId)
            .OrderBy(pa => pa.AssessmentType)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PeriodicAssessment>> GetByEnrollmentAndTypesAsync(
        int enrollmentId,
        IEnumerable<PeriodicAssessmentType> types,
        CancellationToken ct = default)
    {
        var typeList = types.ToList();
        return await _context.PeriodicAssessments
            .Where(pa =>
                pa.EnrollmentId == enrollmentId &&
                typeList.Contains(pa.AssessmentType))
            .OrderBy(pa => pa.AssessmentType)
            .ToListAsync(ct);
    }


    public async Task<IReadOnlyList<PeriodicAssessment>> GetByClassAndTypeAsync(
        int classId,
        PeriodicAssessmentType assessmentType,
        AcademicTerm? term = null,
        CancellationToken ct = default)
    {
        var query = _context.PeriodicAssessments
            .Where(pa =>
                pa.AssessmentType == assessmentType &&
                pa.Enrollment.ClassId == classId &&
                pa.Enrollment.LeftAt == null);

        if (term.HasValue)
            query = query.Where(pa => pa.Term == term.Value);
        else
            query = query.Where(pa => pa.Term == null);

        return await query
            .Include(pa => pa.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(pa => pa.Score)
            .ToListAsync(ct);
    }


    public async Task UpsertAsync(PeriodicAssessment assessment, CancellationToken ct = default)
    {
        var existing = await _context.PeriodicAssessments
            .FirstOrDefaultAsync(pa =>
                pa.EnrollmentId == assessment.EnrollmentId &&
                pa.AssessmentType == assessment.AssessmentType &&
                pa.Term == assessment.Term, ct);

        if (existing is null)
            await _context.PeriodicAssessments.AddAsync(assessment, ct);
        else
        {
            existing.Score          = assessment.Score;
            existing.MaxScore       = assessment.MaxScore;
            existing.AssessmentDate = assessment.AssessmentDate;
            existing.UpdatedAt      = DateTime.UtcNow;
        }
    }

    public async Task BulkUpsertAsync(
        IEnumerable<PeriodicAssessment> assessments,
        CancellationToken ct = default)
    {
        var list = assessments.ToList();
        if (!list.Any()) return;

        var keys = list.Select(a => new { a.EnrollmentId, a.AssessmentType, a.Term, a.SubjectId }).Distinct().ToList();
        var enrollmentIds = keys.Select(k => k.EnrollmentId).Distinct().ToList();

        var existing = await _context.PeriodicAssessments
            .Where(pa => enrollmentIds.Contains(pa.EnrollmentId))
            .ToListAsync(ct);

        foreach (var assessment in list)
        {
            var ex = existing.FirstOrDefault(pa =>
                pa.EnrollmentId == assessment.EnrollmentId &&
                pa.AssessmentType == assessment.AssessmentType &&
                pa.Term == assessment.Term &&
                pa.SubjectId == assessment.SubjectId);

            if (ex is null)
                await _context.PeriodicAssessments.AddAsync(assessment, ct);
            else
            {
                ex.Score          = assessment.Score;
                ex.MaxScore       = assessment.MaxScore;
                ex.AssessmentDate = assessment.AssessmentDate;
                ex.UpdatedAt      = DateTime.UtcNow;
            }
        }
    }
}
