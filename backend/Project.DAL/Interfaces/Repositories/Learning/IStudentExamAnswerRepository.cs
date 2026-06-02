using Project.Domain.Entities;
using Project.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IStudentExamAnswerRepository : IRepository<StudentExamAnswer>
{
    Task<IReadOnlyList<StudentExamAnswer>> GetByAttemptIdAsync(int attemptId, CancellationToken ct = default);
    Task<StudentExamAnswer?>               GetByAttemptAndQuestionAsync(int attemptId, int questionId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentExamAnswer>> GetWithQuestionsAsync(int attemptId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentExamAnswer>> GetIncorrectAnswersAsync(int attemptId, CancellationToken ct = default);
}



