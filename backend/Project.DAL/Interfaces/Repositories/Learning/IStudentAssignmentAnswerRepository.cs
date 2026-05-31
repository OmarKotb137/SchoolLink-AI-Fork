using SchoolLink.Domain.Entities;
using SchoolLink.Domain.Enums;
using System.Linq.Expressions;

namespace Project.DAL.Interfaces.Repositories.Learning;

public interface IStudentAssignmentAnswerRepository : IRepository<StudentAssignmentAnswer>
{
    Task<IReadOnlyList<StudentAssignmentAnswer>> GetBySubmissionIdAsync(int submissionId, CancellationToken ct = default);
    Task<StudentAssignmentAnswer?>               GetBySubmissionAndQuestionAsync(int submissionId, int questionId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentAssignmentAnswer>> GetWithQuestionsAsync(int submissionId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentAssignmentAnswer>> GetIncorrectAnswersAsync(int submissionId, CancellationToken ct = default);
}



