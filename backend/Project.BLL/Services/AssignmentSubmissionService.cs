using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.AssignmentSubmission;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services
{
    public class AssignmentSubmissionService : IAssignmentSubmissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public AssignmentSubmissionService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OperationResult<GetAssignmentSubmissionDto>> GetByIdAsync(int id)
        {
            var submission = await _unitOfWork.StudentAssignmentSubmissions.GetWithAnswersAsync(id);

            if (submission == null || submission.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("Submission not found", 404);

            var dto = _mapper.Map<GetAssignmentSubmissionDto>(submission);
            return OperationResult<GetAssignmentSubmissionDto>.Success(dto);
        }

        public async Task<OperationResult<List<AssignmentSubmissionSummaryDto>>> GetByAssignmentIdAsync(int assignmentId)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);

            if (assignment == null || assignment.IsDeleted)
                return OperationResult<List<AssignmentSubmissionSummaryDto>>.Failure("Assignment not found", 404);

            var submissions = await _unitOfWork.StudentAssignmentSubmissions
                .GetByAssignmentIdAsync(assignmentId);

            var dtos = _mapper.Map<List<AssignmentSubmissionSummaryDto>>(submissions);
            return OperationResult<List<AssignmentSubmissionSummaryDto>>.Success(dtos);
        }

        public async Task<OperationResult<GetAssignmentSubmissionDto>> SubmitAsync(CreateAssignmentSubmissionDto dto)
        {
            // Check enrollment exists
            var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(dto.EnrollmentId);
            if (enrollment == null || enrollment.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("Enrollment not found", 404);

            // Check assignment exists
            var assignment = await _unitOfWork.Assignments.GetWithQuestionsAsync(dto.AssignmentId);
            if (assignment == null || assignment.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("Assignment not found", 404);

            // Check due date
            if (assignment.DueDate.HasValue && assignment.DueDate < DateTime.UtcNow)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("Assignment due date has passed");

            // Check already submitted
            var existingSubmission = await _unitOfWork.StudentAssignmentSubmissions
                .GetByEnrollmentAndAssignmentAsync(dto.EnrollmentId, dto.AssignmentId);

            if (existingSubmission != null)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("Assignment already submitted");

            // Create submission
            var submission = new StudentAssignmentSubmission
            {
                EnrollmentId = dto.EnrollmentId,
                AssignmentId = dto.AssignmentId,
                SubmittedAt = DateTime.UtcNow,
                MaxScore = assignment.MaxScore,
                IsGraded = false,
                Answers = dto.Answers.Select(a => new StudentAssignmentAnswer
                {
                    QuestionId = a.QuestionId,
                    AnswerText = a.AnswerText,
                    PointsEarned = 0
                }).ToList()
            };

            // Auto grade MCQ and TrueFalse
            if (assignment.IsAutoGraded)
            {
                decimal totalPoints = 0;
                foreach (var answer in submission.Answers)
                {
                    var question = assignment.Questions
                        .FirstOrDefault(q => q.Id == answer.QuestionId);

                    if (question == null) continue;

                    if (question.QuestionType == QuestionType.MultipleChoice ||
                        question.QuestionType == QuestionType.TrueFalse)
                    {
                        var isCorrect = question.CorrectAnswer?
                            .Equals(answer.AnswerText, StringComparison.OrdinalIgnoreCase) ?? false;

                        answer.IsCorrect = isCorrect;
                        answer.PointsEarned = isCorrect ? question.Points : 0;
                        totalPoints += answer.PointsEarned;
                    }
                }

                submission.Score = totalPoints;
                submission.IsGraded = true;
            }

            await _unitOfWork.StudentAssignmentSubmissions.AddAsync(submission);
            await _unitOfWork.SaveChangesAsync();

            var resultDto = _mapper.Map<GetAssignmentSubmissionDto>(submission);
            return OperationResult<GetAssignmentSubmissionDto>.Success(resultDto, "Submission created successfully");
        }

        public async Task<OperationResult> GradeAsync(int submissionId)
        {
            var submission = await _unitOfWork.StudentAssignmentSubmissions
                .GetWithAnswersAsync(submissionId);

            if (submission == null || submission.IsDeleted)
                return OperationResult.Failure("Submission not found");

            if (submission.IsGraded)
                return OperationResult.Failure("Submission already graded");

            decimal totalPoints = 0;
            foreach (var answer in submission.Answers)
            {
                totalPoints += answer.PointsEarned;
            }

            submission.Score = totalPoints;
            submission.IsGraded = true;
            submission.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.StudentAssignmentSubmissions.Update(submission);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("Submission graded successfully");
        }
    }
}