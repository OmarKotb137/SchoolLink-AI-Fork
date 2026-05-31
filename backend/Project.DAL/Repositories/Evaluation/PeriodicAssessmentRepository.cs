using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class PeriodicAssessmentRepository : Repository<PeriodicAssessment>, IPeriodicAssessmentRepository
{
    public PeriodicAssessmentRepository(AppDbContext context) : base(context) { }


    public async Task<PeriodicAssessment?> GetByEnrollmentAndTypeAsync(
        int enrollmentId,
        PeriodicAssessmentType assessmentType,
        CancellationToken ct = default)
        => await _context.PeriodicAssessments
            .FirstOrDefaultAsync(pa =>
                pa.EnrollmentId    == enrollmentId    &&
                pa.AssessmentType  == assessmentType, ct);


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
        CancellationToken ct = default)
        => await _context.PeriodicAssessments
            .Where(pa =>
                pa.AssessmentType        == assessmentType &&
                pa.Enrollment.ClassId    == classId        &&
                pa.Enrollment.LeftAt     == null)
            .Include(pa => pa.Enrollment)
                .ThenInclude(e => e.Student)
            .OrderByDescending(pa => pa.Score)
            .ToListAsync(ct);


    public async Task UpsertAsync(PeriodicAssessment assessment, CancellationToken ct = default)
    {
        var existing = await _context.PeriodicAssessments
            .FirstOrDefaultAsync(pa =>
                pa.EnrollmentId   == assessment.EnrollmentId   &&
                pa.AssessmentType == assessment.AssessmentType, ct);

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

        var enrollmentIds = list.Select(a => a.EnrollmentId).Distinct().ToList();
        var types         = list.Select(a => a.AssessmentType).Distinct().ToList();

        var existing = await _context.PeriodicAssessments
            .Where(pa =>
                enrollmentIds.Contains(pa.EnrollmentId) &&
                types.Contains(pa.AssessmentType))
            .ToListAsync(ct);

        foreach (var assessment in list)
        {
            var ex = existing.FirstOrDefault(pa =>
                pa.EnrollmentId   == assessment.EnrollmentId   &&
                pa.AssessmentType == assessment.AssessmentType);

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



