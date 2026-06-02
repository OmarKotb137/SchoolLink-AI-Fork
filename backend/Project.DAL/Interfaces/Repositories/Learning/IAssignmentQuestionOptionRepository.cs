using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IAssignmentQuestionOptionRepository : IRepository<AssignmentQuestionOption>
{
    Task<IReadOnlyList<AssignmentQuestionOption>> GetByQuestionIdAsync(int questionId, CancellationToken ct = default);
    Task<AssignmentQuestionOption?>               GetCorrectOptionAsync(int questionId, CancellationToken ct = default);
}



