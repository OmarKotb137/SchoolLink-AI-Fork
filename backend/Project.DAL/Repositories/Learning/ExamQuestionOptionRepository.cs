using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class ExamQuestionOptionRepository
    : Repository<ExamQuestionOption>, IExamQuestionOptionRepository
{
    public ExamQuestionOptionRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ExamQuestionOption>> GetByQuestionIdAsync(
        int questionId,
        CancellationToken ct = default)
        => await _context.ExamQuestionOptions
            .Where(o => o.QuestionId == questionId)
            .OrderBy(o => o.DisplayOrder)
            .ToListAsync(ct);

    public async Task<ExamQuestionOption?> GetCorrectOptionAsync(
        int questionId,
        CancellationToken ct = default)
        => await _context.ExamQuestionOptions
            .FirstOrDefaultAsync(o =>
                o.QuestionId == questionId &&
                o.IsCorrect, ct);
}



