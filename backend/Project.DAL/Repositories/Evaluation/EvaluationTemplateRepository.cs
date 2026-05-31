using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Evaluation;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Evaluation;

public class EvaluationTemplateRepository : Repository<EvaluationTemplate>, IEvaluationTemplateRepository
{
    public EvaluationTemplateRepository(AppDbContext context) : base(context) { }


    public async Task<EvaluationTemplate?> GetByGradeLevelSubjectAndYearAsync(
        int gradeLevelId,
        int subjectId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationTemplates
            .Include(t => t.GradeLevel)
            .Include(t => t.Subject)
            .FirstOrDefaultAsync(t =>
                t.GradeLevelId == gradeLevelId &&
                t.SubjectId == subjectId &&
                t.AcademicYearId == academicYearId, ct);


    public async Task<IReadOnlyList<EvaluationTemplate>> GetByGradeLevelAndYearAsync(
        int gradeLevelId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationTemplates
            .Where(t =>
                t.GradeLevelId == gradeLevelId &&
                t.AcademicYearId == academicYearId)
            .Include(t => t.Subject)
            .OrderBy(t => t.Subject.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvaluationTemplate>> GetByAcademicYearAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationTemplates
            .Where(t => t.AcademicYearId == academicYearId)
            .Include(t => t.GradeLevel)
            .Include(t => t.Subject)
            .OrderBy(t => t.GradeLevel.LevelOrder)
            .ThenBy(t => t.Subject.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<EvaluationTemplate>> GetActiveAsync(
        CancellationToken ct = default)
        => await _context.EvaluationTemplates
            .Where(t => t.IsActive)
            .Include(t => t.GradeLevel)
            .Include(t => t.Subject)
            .OrderBy(t => t.GradeLevel.LevelOrder)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);


    public async Task<EvaluationTemplate?> GetWithItemsAsync(
        int templateId,
        CancellationToken ct = default)
        => await _context.EvaluationTemplates
            .Include(t => t.Items
                .Where(i => i.IsVisible)
                .OrderBy(i => i.DisplayOrder))
            .Include(t => t.GradeLevel)
            .Include(t => t.Subject)
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);


    public async Task<bool> ExistsByGradeLevelSubjectAndYearAsync(
        int gradeLevelId,
        int subjectId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.EvaluationTemplates
            .AnyAsync(t =>
                t.GradeLevelId == gradeLevelId &&
                t.SubjectId == subjectId &&
                t.AcademicYearId == academicYearId, ct);
}



