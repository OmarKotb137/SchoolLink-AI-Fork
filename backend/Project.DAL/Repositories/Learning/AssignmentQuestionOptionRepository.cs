using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class AssignmentQuestionOptionRepository
    : Repository<AssignmentQuestionOption>, IAssignmentQuestionOptionRepository
{
    public AssignmentQuestionOptionRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AssignmentQuestionOption>> GetByQuestionIdAsync(
        int questionId,
        CancellationToken ct = default)
        => await _context.AssignmentQuestionOptions
            .Where(o => o.QuestionId == questionId)
            .OrderBy(o => o.DisplayOrder)
            .ToListAsync(ct);

    public async Task<AssignmentQuestionOption?> GetCorrectOptionAsync(
        int questionId,
        CancellationToken ct = default)
        => await _context.AssignmentQuestionOptions
            .FirstOrDefaultAsync(o =>
                o.QuestionId == questionId &&
                o.IsCorrect, ct);
}



