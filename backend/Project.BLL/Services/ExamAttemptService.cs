using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.ExamAttempt;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

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
                return OperationResult<GetExamAttemptDto>.Failure("المحاولة غير موجودة", 404);

            var dto = _mapper.Map<GetExamAttemptDto>(attempt);
            return OperationResult<GetExamAttemptDto>.Success(dto);
        }

        public async Task<OperationResult<List<ExamAttemptSummaryDto>>> GetByExamIdAsync(int examId, int teacherId)
        {
            var exam = await _unitOfWork.Exams.GetByIdAsync(examId);

            if (exam == null || exam.IsDeleted)
                return OperationResult<List<ExamAttemptSummaryDto>>.Failure("الامتحان غير موجود", 404);

            // Phase 6.2 — فحص الملكية: المعلم لازم يكون صاحب هذا الامتحان
            var examWithCst = await _unitOfWork.Exams.GetWithClassSubjectTeacherAsync(examId);
            if (examWithCst?.ClassSubjectTeacher?.TeacherId != null
                && examWithCst.ClassSubjectTeacher.TeacherId != teacherId)
                return OperationResult<List<ExamAttemptSummaryDto>>.Failure("غير مصرح لك بعرض نتائج هذا الامتحان", 403);

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
                return OperationResult<GetExamAttemptDto>.Failure("التسجيل غير موجود", 404);

            var exam = await _unitOfWork.Exams.GetByIdAsync(dto.ExamId);

            if (exam == null || exam.IsDeleted)
                return OperationResult<GetExamAttemptDto>.Failure("الامتحان غير موجود", 404);

            if (!exam.IsPublished)
                return OperationResult<GetExamAttemptDto>.Failure("الامتحان غير منشور", 400);

            var now = DateTime.UtcNow;
            if (exam.StartTime.HasValue && now < exam.StartTime)
                return OperationResult<GetExamAttemptDto>.Failure("الامتحان لم يبدأ بعد", 400);

            if (exam.EndTime.HasValue && now > exam.EndTime)
                return OperationResult<GetExamAttemptDto>.Failure("الامتحان قد انتهى بالفعل", 400);

            var alreadyAttempted = await _unitOfWork.StudentExamAttempts
                .HasAttemptedAsync(dto.EnrollmentId, dto.ExamId, CancellationToken.None);

            if (alreadyAttempted)
                return OperationResult<GetExamAttemptDto>.Failure("محاولة لهذا الامتحان موجودة بالفعل", 400);

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
            return OperationResult<GetExamAttemptDto>.Success(resultDto, "تم بدء المحاولة بنجاح");
        }

        public async Task<OperationResult<GetExamAttemptDto>> SubmitAttemptAsync(SubmitExamAttemptDto dto)
        {
            var attempt = await _unitOfWork.StudentExamAttempts
                .GetWithAnswersAsync(dto.AttemptId, CancellationToken.None);

            if (attempt == null || attempt.IsDeleted)
                return OperationResult<GetExamAttemptDto>.Failure("المحاولة غير موجودة", 404);

            if (attempt.SubmittedAt != null)
                return OperationResult<GetExamAttemptDto>.Failure("تم تقديم المحاولة بالفعل", 400);

            var exam = await _unitOfWork.Exams
                .GetWithQuestionsAsync(attempt.ExamId, CancellationToken.None);

            if (exam == null || exam.IsDeleted)
                return OperationResult<GetExamAttemptDto>.Failure("الامتحان غير موجود", 404);

            // check time limit
            if (exam.DurationMinutes.HasValue)
            {
                var elapsed = (DateTime.UtcNow - attempt.StartedAt).TotalMinutes;
                if (elapsed > exam.DurationMinutes.Value)
                    return OperationResult<GetExamAttemptDto>.Failure("تم تجاوز الوقت المحدد للامتحان", 400);
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
            return OperationResult<GetExamAttemptDto>.Success(resultDto, "تم تقديم الامتحان بنجاح");
        }

        public async Task<OperationResult<List<ExamAttemptSummaryDto>>> GetStudentAttemptsAsync(int enrollmentId, int examId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<List<ExamAttemptSummaryDto>>.Failure("التسجيل غير موجود", 404);

        var allAttempts = await _unitOfWork.StudentExamAttempts.GetByEnrollmentIdAsync(enrollmentId);
        var filtered = allAttempts.Where(a => a.ExamId == examId && !a.IsDeleted).ToList();

        var dtos = _mapper.Map<List<ExamAttemptSummaryDto>>(filtered);
        return OperationResult<List<ExamAttemptSummaryDto>>.Success(dtos, "تم جلب محاولات الطالب بنجاح");
    }

    public async Task<OperationResult> AutoGradeAsync(int attemptId)
    {
        var attempt = await _unitOfWork.StudentExamAttempts
            .GetWithAnswersAsync(attemptId, CancellationToken.None);

        if (attempt == null || attempt.IsDeleted)
            return OperationResult.Failure("المحاولة غير موجودة", 404);

        if (attempt.SubmittedAt == null)
            return OperationResult.Failure("لم يتم تقديم المحاولة بعد");

        if (attempt.IsGraded)
            return OperationResult.Failure("تم تصحيح المحاولة بالفعل");

        var exam = await _unitOfWork.Exams.GetWithQuestionsAsync(attempt.ExamId, CancellationToken.None);
        if (exam == null || exam.IsDeleted)
            return OperationResult.Failure("الامتحان غير موجود", 404);

        decimal totalScore = 0;
        foreach (var answer in attempt.Answers)
        {
            var question = exam.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
            if (question == null) continue;

            if (question.QuestionType is QuestionType.MultipleChoice or QuestionType.TrueFalse)
            {
                var isCorrect = !string.IsNullOrEmpty(question.CorrectAnswer) &&
                                answer.AnswerText?.Trim().Equals(question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase) == true;

                answer.IsCorrect = isCorrect;
                answer.PointsEarned = isCorrect ? question.Points : 0;
                totalScore += answer.PointsEarned;
                _unitOfWork.StudentExamAnswers.Update(answer);
            }
        }

        attempt.Score = totalScore;
        attempt.IsGraded = true;
        attempt.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.StudentExamAttempts.Update(attempt);
        await _unitOfWork.SaveChangesAsync(CancellationToken.None);

        return OperationResult.Success("تم تصحيح المحاولة تلقائياً بنجاح");
    }

    public async Task<OperationResult> GradeEssayAnswersAsync(int attemptId, GradeEssayAttemptDto dto, int teacherId)
        {
            var attempt = await _unitOfWork.StudentExamAttempts
                .GetWithAnswersAsync(attemptId, CancellationToken.None);

            if (attempt == null || attempt.IsDeleted)
                return OperationResult.Failure("المحاولة غير موجودة", 404);

            if (attempt.SubmittedAt == null)
                return OperationResult.Failure("لم يتم تقديم المحاولة بعد");

            // فحص ملكية: المعلم لازم يكون صاحب هذا الامتحان
            var examWithCst = await _unitOfWork.Exams.GetWithClassSubjectTeacherAsync(attempt.ExamId);
            if (examWithCst?.ClassSubjectTeacher?.TeacherId != null
                && examWithCst.ClassSubjectTeacher.TeacherId != teacherId)
                return OperationResult.Failure("غير مصرح لك بتصحيح هذه المحاولة", 403);

            var exam = await _unitOfWork.Exams.GetWithQuestionsAsync(attempt.ExamId, CancellationToken.None);
            if (exam == null) return OperationResult.Failure("الامتحان غير موجود", 404);

            foreach (var ansDto in dto.Answers)
            {
                var answer = attempt.Answers.FirstOrDefault(a => a.Id == ansDto.AnswerId);
                if (answer == null) continue;

                var question = exam.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question == null || question.QuestionType != Project.Domain.Enums.QuestionType.Essay) continue;

                // منع إدخال درجة أعلى من الحد الأقصى
                var earned = Math.Min(ansDto.PointsEarned, question.Points);
                answer.PointsEarned = earned;
                answer.IsCorrect    = earned > 0;
                answer.AIFeedback   = ansDto.Feedback;   // يُستخدم كملاحظة المعلم
                _unitOfWork.StudentExamAnswers.Update(answer);
            }

            // إعادة حساب مجموع الدرجة
            attempt.Score = attempt.Answers.Sum(a => a.PointsEarned);

            // تحقق: هل لسه أسئلة مقالية بدون درجة محددة
            var hasUngradedEssay = attempt.Answers
                .Join(exam.Questions, a => a.QuestionId, q => q.Id, (a, q) => new { a, q })
                .Any(x => x.q.QuestionType == Project.Domain.Enums.QuestionType.Essay
                       && !x.a.IsCorrect.HasValue);

            if (!hasUngradedEssay)
                attempt.IsGraded = true;

            attempt.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.StudentExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("تم حفظ التصحيح بنجاح");
        }

        // Legacy — kept to avoid breaking the auto-grade endpoint; candidates for removal
        public async Task<OperationResult> GradeAttemptAsync_Legacy(int attemptId)
        {
            var attempt = await _unitOfWork.StudentExamAttempts
                .GetWithAnswersAsync(attemptId, CancellationToken.None);

            if (attempt == null || attempt.IsDeleted)
                return OperationResult.Failure("المحاولة غير موجودة");

            if (attempt.SubmittedAt == null)
                return OperationResult.Failure("لم يتم تقديم المحاولة بعد");

            if (attempt.IsGraded)
                return OperationResult.Failure("تم تصحيح المحاولة بالفعل");

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

            return OperationResult.Success("تم تصحيح المحاولة بنجاح");
        }
    }
}