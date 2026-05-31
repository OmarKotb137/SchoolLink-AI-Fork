using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class AssignmentQuestionRepository : Repository<AssignmentQuestion>, IAssignmentQuestionRepository
{
    public AssignmentQuestionRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<AssignmentQuestion>> GetByAssignmentIdAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.AssignmentQuestions
            .Where(q => q.AssignmentId == assignmentId)
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<AssignmentQuestion>> GetWithOptionsByAssignmentIdAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.AssignmentQuestions
            .Where(q => q.AssignmentId == assignmentId)
            .Include(q => q.Options
                .OrderBy(o => o.DisplayOrder))
            .OrderBy(q => q.DisplayOrder)
            .ToListAsync(ct);

    public async Task<int> GetCountByAssignmentAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.AssignmentQuestions
            .CountAsync(q => q.AssignmentId == assignmentId, ct);

    public async Task<decimal> GetTotalPointsByAssignmentAsync(
        int assignmentId,
        CancellationToken ct = default)
        => await _context.AssignmentQuestions
            .Where(q => q.AssignmentId == assignmentId)
            .SumAsync(q => q.Points, ct);
}



