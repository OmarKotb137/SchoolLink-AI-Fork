using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IExamQuestionOptionRepository : IRepository<ExamQuestionOption>
{
    Task<IReadOnlyList<ExamQuestionOption>> GetByQuestionIdAsync(int questionId, CancellationToken ct = default);
    Task<ExamQuestionOption?>               GetCorrectOptionAsync(int questionId, CancellationToken ct = default);
}



