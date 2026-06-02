using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.ExamAttempt;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services
{
    public class ExamAttemptService : IExamAttemptService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ExamAttemptService(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<OperationResult<GetExamAttemptDto>> GetByIdAsync(int id)
        {
            var attempt = await _unitOfWork.StudentExamAttempts
                .GetWithAnswersAsync(id, CancellationToken.None);

            if (attempt == null || attempt.IsDeleted)
                return OperationResult<GetExamAttemptDto>.Failure("Attempt not found", 404);

            var dto = _mapper.Map<GetExamAttemptDto>(attempt);
            return OperationResult<GetExamAttemptDto>.Success(dto);
        }

        public async Task<OperationResult<List<ExamAttemptSummaryDto>>> GetByExamIdAsync(int examId)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.IsDeleted)
                return OperationResult<List<ExamAttemptSummaryDto>>.Failure("Exam not found", 404);

            var attempts = await _unitOfWork.StudentExamAttempts
                .GetByExamIdAsync(examId, CancellationToken.None);

            var dtos = _mapper.Map<List<ExamAttemptSummaryDto>>(attempts);
            return OperationResult<List<ExamAttemptSummaryDto>>.Success(dtos);
        }

        public async Task<OperationResult<GetExamAttemptDto>> StartAttemptAsync(CreateExamAttemptDto dto)
        {
            var enrollment = await _unitOfWork.StudentEnrollments
                .GetByIdAsync(dto.EnrollmentId);

            if (enrollment == null || enrollment.IsDeleted)
                return OperationResult<GetExamAttemptDto>.Failure("Enrollment not found", 404);

            var exam = await _unitOfWork.Exams.GetByIdAsync(dto.ExamId);

            if (exam == null || exam.IsDeleted)
                return OperationResult<GetExamAttemptDto>.Failure("Exam not found", 404);

            if (!exam.IsPublished)
                return OperationResult<GetExamAttemptDto>.Failure("Exam is not published", 400);

            var now = DateTime.UtcNow;
            if (exam.StartTime.HasValue && now < exam.StartTime)
                return OperationResult<GetExamAttemptDto>.Failure("Exam has not started yet", 400);

            if (exam.EndTime.HasValue && now > exam.EndTime)
                return OperationResult<GetExamAttemptDto>.Failure("Exam has already ended", 400);

            var alreadyAttempted = await _unitOfWork.StudentExamAttempts
                .HasAttemptedAsync(dto.EnrollmentId, dto.ExamId, CancellationToken.None);

            if (alreadyAttempted)
                return OperationResult<GetExamAttemptDto>.Failure("Attempt already exists for this exam", 400);

            var attempt = new StudentExamAttempt
            {
                EnrollmentId = dto.EnrollmentId,
                ExamId = dto.ExamId,
                TotalScore = exam.TotalScore,
                StartedAt = now
            };

            await _unitOfWork.StudentExamAttempts.AddAsync(attempt);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            var resultDto = _mapper.Map<GetExamAttemptDto>(attempt);
            return OperationResult<GetExamAttemptDto>.Success(resultDto, "Exam attempt started successfully");
        }

        public async Task<OperationResult<GetExamAttemptDto>> SubmitAttemptAsync(SubmitExamAttemptDto dto)
        {
            var attempt = await _unitOfWork.StudentExamAttempts
                .GetWithAnswersAsync(dto.AttemptId, CancellationToken.None);

            if (attempt == null || attempt.IsDeleted)
                return OperationResult<GetExamAttemptDto>.Failure("Attempt not found", 404);

            if (attempt.SubmittedAt != null)
                return OperationResult<GetExamAttemptDto>.Failure("Attempt already submitted", 400);

            var exam = await _unitOfWork.Exams
                .GetWithQuestionsAsync(attempt.ExamId, CancellationToken.None);

            if (exam == null)
                return OperationResult<GetExamAttemptDto>.Failure("Exam not found", 404);

            // check time limit
            if (exam.DurationMinutes.HasValue)
            {
                var elapsed = (DateTime.UtcNow - attempt.StartedAt).TotalMinutes;
                if (elapsed > exam.DurationMinutes.Value)
                    return OperationResult<GetExamAttemptDto>.Failure("Exam time limit exceeded", 400);
            }

            // save answers
            foreach (var answerDto in dto.Answers)
            {
                var question = exam.Questions
                    .FirstOrDefault(q => q.Id == answerDto.QuestionId && !q.IsDeleted);

                if (question == null) continue;

                var answer = new StudentExamAnswer
                {
                    AttemptId = attempt.Id,
                    QuestionId = question.Id,
                    AnswerText = answerDto.AnswerText
                };

                await _unitOfWork.StudentExamAnswers.AddAsync(answer);
            }

            attempt.SubmittedAt = DateTime.UtcNow;

            _unitOfWork.StudentExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            // reload with answers
            var updatedAttempt = await _unitOfWork.StudentExamAttempts
                .GetWithAnswersAsync(attempt.Id, CancellationToken.None);

            var resultDto = _mapper.Map<GetExamAttemptDto>(updatedAttempt!);
            return OperationResult<GetExamAttemptDto>.Success(resultDto, "Exam submitted successfully");
        }

        public async Task<OperationResult> GradeAttemptAsync(int attemptId)
        {
            var attempt = await _unitOfWork.StudentExamAttempts
                .GetWithAnswersAsync(attemptId, CancellationToken.None);

            if (attempt == null || attempt.IsDeleted)
                return OperationResult.Failure("Attempt not found");

            if (attempt.SubmittedAt == null)
                return OperationResult.Failure("Attempt has not been submitted yet");

            if (attempt.IsGraded)
                return OperationResult.Failure("Attempt is already graded");

            decimal totalScore = 0;

            foreach (var answer in attempt.Answers)
            {
                if (answer.Question == null) continue;

                var isCorrect = !string.IsNullOrEmpty(answer.Question.CorrectAnswer) &&
                                answer.AnswerText?.Trim().ToLower() ==
                                answer.Question.CorrectAnswer.Trim().ToLower();

                answer.IsCorrect = isCorrect;
                answer.PointsEarned = isCorrect ? answer.Question.Points : 0;
                totalScore += answer.PointsEarned;

                _unitOfWork.StudentExamAnswers.Update(answer);
            }

            attempt.Score = totalScore;
            attempt.IsGraded = true;
            attempt.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.StudentExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("Attempt graded successfully");
        }
    }
}