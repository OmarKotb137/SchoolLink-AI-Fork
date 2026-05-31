using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IExamQuestionRepository : IRepository<ExamQuestion>
{
    Task<IReadOnlyList<ExamQuestion>> GetByExamIdAsync(int examId, CancellationToken ct = default);
    Task<IReadOnlyList<ExamQuestion>> GetWithOptionsByExamIdAsync(int examId, CancellationToken ct = default);
    Task<int>                         GetCountByExamAsync(int examId, CancellationToken ct = default);
    Task<decimal>                     GetTotalPointsByExamAsync(int examId, CancellationToken ct = default);
}



