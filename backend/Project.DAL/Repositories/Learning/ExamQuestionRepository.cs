using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class ExamQuestionRepository : Repository<ExamQuestion>, IExamQuestionRepository
{
    public ExamQuestionRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ExamQuestion>> GetByExamIdAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.ExamQuestions
            .Where(q => q.ExamId == examId)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ExamQuestion>> GetWithOptionsByExamIdAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.ExamQuestions
            .Where(q => q.ExamId == examId)
            .Include(q => q.Options
                .OrderBy(o => o.DisplayOrder))
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(ct);

    public async Task<int> GetCountByExamAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.ExamQuestions
            .CountAsync(q => q.ExamId == examId, ct);

    public async Task<decimal> GetTotalPointsByExamAsync(
        int examId,
        CancellationToken ct = default)
        => await _context.ExamQuestions
            .Where(q => q.ExamId == examId)
            .SumAsync(q => q.Points, ct);
}



