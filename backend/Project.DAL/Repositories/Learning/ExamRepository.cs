using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class ExamRepository : Repository<Exam>, IExamRepository
{
    public ExamRepository(AppDbContext context) : base(context) { }


    public async Task<IReadOnlyList<Exam>> GetByClassSubjectTeacherIdAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.Exams
            .Where(e => e.ClassSubjectTeacherId == classSubjectTeacherId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Exam>> GetByCategoryAsync(
        int classSubjectTeacherId,
        EvaluationCategory category,
        CancellationToken ct = default)
        => await _context.Exams
            .Where(e =>
                e.ClassSubjectTeacherId == classSubjectTeacherId &&
                e.Category              == category)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Exam>> GetAIGeneratedAsync(
        int classSubjectTeacherId,
        CancellationToken ct = default)
        => await _context.Exams
            .Where(e =>
                e.ClassSubjectTeacherId == classSubjectTeacherId &&
                e.IsAIGenerated)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Exam>> GetPublishedByClassIdAsync(
        int classId,
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Exams
            .Where(e =>
                e.ClassSubjectTeacher.ClassId         == classId        &&
                e.ClassSubjectTeacher.AcademicYearId  == academicYearId &&
                e.IsPublished)
            .Include(e => e.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Exam>> GetUpcomingByClassAsync(
        int classId,
        int days,
        CancellationToken ct = default)
    {
        var now    = DateTime.UtcNow;
        var cutoff = now.AddDays(days);

        return await _context.Exams
            .Where(e =>
                e.ClassSubjectTeacher.ClassId == classId &&
                e.IsPublished                            &&
                e.StartTime.HasValue                     &&
                e.StartTime >= now                       &&
                e.StartTime <= cutoff)
            .Include(e => e.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderBy(e => e.StartTime)
            .ToListAsync(ct);
    }


    public async Task<IReadOnlyList<Exam>> GetActiveExamsAsync(
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        return await _context.Exams
            .Where(e =>
                e.IsPublished      &&
                e.StartTime.HasValue &&
                e.EndTime.HasValue   &&
                e.StartTime <= now   &&
                e.EndTime   >= now)
            .Include(e => e.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderBy(e => e.EndTime)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Exam>> GetByDateRangeAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
        => await _context.Exams
            .Where(e =>
                e.StartTime.HasValue &&
                e.StartTime >= from  &&
                e.StartTime <= to)
            .Include(e => e.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderBy(e => e.StartTime)
            .ToListAsync(ct);


    public async Task<IReadOnlyList<Exam>> GetByAcademicYearAsync(
        int academicYearId,
        CancellationToken ct = default)
        => await _context.Exams
            .Where(e => e.ClassSubjectTeacher.AcademicYearId == academicYearId)
            .Include(e => e.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Class)
                    .ThenInclude(c => c.GradeLevel)
            .Include(e => e.ClassSubjectTeacher)
                .ThenInclude(cst => cst.Subject)
            .OrderByDescending(e => e.StartTime)
            .ToListAsync(ct);


    public async Task<Exam?> GetWithQuestionsAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.Exams
            .Include(e => e.Questions
                .OrderBy(q => q.DisplayOrder))
                .ThenInclude(q => q.Options
                    .OrderBy(o => o.DisplayOrder))
            .FirstOrDefaultAsync(e => e.Id == examId, ct);
}



