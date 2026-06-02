using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IAssignmentQuestionRepository : IRepository<AssignmentQuestion>
{
    Task<IReadOnlyList<AssignmentQuestion>> GetByAssignmentIdAsync(int assignmentId, CancellationToken ct = default);
    Task<IReadOnlyList<AssignmentQuestion>> GetWithOptionsByAssignmentIdAsync(int assignmentId, CancellationToken ct = default);
    Task<int>                              GetCountByAssignmentAsync(int assignmentId, CancellationToken ct = default);
    Task<decimal>                          GetTotalPointsByAssignmentAsync(int assignmentId, CancellationToken ct = default);
}



