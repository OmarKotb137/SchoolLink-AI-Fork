using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using SchoolLink.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class StudentExamAnswerRepository
    : Repository<StudentExamAnswer>, IStudentExamAnswerRepository
{
    public StudentExamAnswerRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<StudentExamAnswer>> GetByAttemptIdAsync(
        int attemptId,
        CancellationToken ct = default)
        => await _context.StudentExamAnswers
            .Where(a => a.AttemptId == attemptId)
            .Include(a => a.Question)
            .Include(a => a.SelectedOption)
            .OrderBy(a => a.Question.DisplayOrder)
            .ToListAsync(ct);

    public async Task<StudentExamAnswer?> GetByAttemptAndQuestionAsync(
        int attemptId,
        int questionId,
        CancellationToken ct = default)
        => await _context.StudentExamAnswers
            .FirstOrDefaultAsync(a =>
                a.AttemptId  == attemptId  &&
                a.QuestionId == questionId, ct);

    public async Task<IReadOnlyList<StudentExamAnswer>> GetWithQuestionsAsync(
        int attemptId,
        CancellationToken ct = default)
        => await _context.StudentExamAnswers
            .Where(a => a.AttemptId == attemptId)
            .Include(a => a.Question)
                .ThenInclude(q => q.Options
                    .OrderBy(o => o.DisplayOrder))
            .Include(a => a.SelectedOption)
            .OrderBy(a => a.Question.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentExamAnswer>> GetIncorrectAnswersAsync(
        int attemptId,
        CancellationToken ct = default)
        => await _context.StudentExamAnswers
            .Where(a =>
                a.AttemptId == attemptId &&
                a.IsCorrect == false)
            .Include(a => a.Question)
            .Include(a => a.SelectedOption)
            .OrderBy(a => a.Question.DisplayOrder)
            .ToListAsync(ct);
}



