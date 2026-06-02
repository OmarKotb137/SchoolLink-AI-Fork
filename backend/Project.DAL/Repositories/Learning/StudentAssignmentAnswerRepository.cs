using Microsoft.EntityFrameworkCore;
using Project.DAL.Interfaces.Repositories.Learning;
using Project.Domain.Entities;
using Project.DAL.Context;

namespace Project.DAL.Repositories.Learning;

public class StudentAssignmentAnswerRepository
    : Repository<StudentAssignmentAnswer>, IStudentAssignmentAnswerRepository
{
    public StudentAssignmentAnswerRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<StudentAssignmentAnswer>> GetBySubmissionIdAsync(
        int submissionId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentAnswers
            .Where(a => a.SubmissionId == submissionId)
            .Include(a => a.Question)
            .Include(a => a.SelectedOption)
            .OrderBy(a => a.Question.DisplayOrder)
            .ToListAsync(ct);

    public async Task<StudentAssignmentAnswer?> GetBySubmissionAndQuestionAsync(
        int submissionId,
        int questionId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentAnswers
            .FirstOrDefaultAsync(a =>
                a.SubmissionId == submissionId &&
                a.QuestionId   == questionId, ct);

    public async Task<IReadOnlyList<StudentAssignmentAnswer>> GetWithQuestionsAsync(
        int submissionId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentAnswers
            .Where(a => a.SubmissionId == submissionId)
            .Include(a => a.Question)
                .ThenInclude(q => q.Options
                    .OrderBy(o => o.DisplayOrder))
            .Include(a => a.SelectedOption)
            .OrderBy(a => a.Question.DisplayOrder)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StudentAssignmentAnswer>> GetIncorrectAnswersAsync(
        int submissionId,
        CancellationToken ct = default)
        => await _context.StudentAssignmentAnswers
            .Where(a =>
                a.SubmissionId == submissionId &&
                a.IsCorrect    == false)
            .Include(a => a.Question)
            .Include(a => a.SelectedOption)
            .OrderBy(a => a.Question.DisplayOrder)
            .ToListAsync(ct);
}



