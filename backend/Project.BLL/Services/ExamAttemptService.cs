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

            // فحص الملكية/الصلاحية: CST محدد → صاحبه، CST=null → من يُدرّس المادة
            var authorized = await IsTeacherAuthorizedForExamAsync(exam, teacherId);
            if (!authorized)
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
        // عَلَّم: هل فيه إجابة أكمل-فراغ غير مطابقة نصياً وتنتظر مراجعة يدوية؟
        bool hasPendingFillBlank = false;

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
            else if (question.QuestionType == QuestionType.FillBlank)
            {
                // لو الطالب لمّا يكتب إجابة مطابقة تماماً = تصحيح تلقائي (الدرجة كاملة)
                // لو مش مطابقة = نعلّمها بانتظار مراجعة المعلم ولا نعتبرها graded نهائياً
                if (string.IsNullOrEmpty(question.CorrectAnswer))
                {
                    hasPendingFillBlank = true;
                    continue;
                }

                var isMatch = answer.AnswerText?.Trim()
                    .Equals(question.CorrectAnswer.Trim(), StringComparison.OrdinalIgnoreCase) == true;

                if (isMatch)
                {
                    answer.IsCorrect = true;
                    answer.PointsEarned = question.Points;
                    totalScore += answer.PointsEarned;
                    _unitOfWork.StudentExamAnswers.Update(answer);
                }
                else
                {
                    // غير مطابقة نصياً → بانتظار مراجعة المعلم (يبقى IsCorrect=null)
                    hasPendingFillBlank = true;
                }
            }
            // Essay: لا يُصحّح تلقائياً (بانتظار المعلم)
        }

        attempt.Score = totalScore;
        // المرحلة 1: لا نعلّم graded لو فيه fill-blank/essay بانتظار المراجعة اليدوية
        attempt.IsGraded = !hasPendingFillBlank
            && !attempt.Answers
                .Join(exam.Questions, a => a.QuestionId, q => q.Id, (a, q) => new { a, q })
                .Any(x => x.q.QuestionType == QuestionType.Essay && !x.a.IsCorrect.HasValue);
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

            // فحص ملكية/الصلاحية للامتحان (CST محدد → صاحبه، CST=null → من يُدرّس المادة)
            var examForAuth = await _unitOfWork.Exams.GetWithClassSubjectTeacherAsync(attempt.ExamId);
            if (examForAuth == null)
                return OperationResult.Failure("الامتحان غير موجود", 404);

            var authorized = await IsTeacherAuthorizedForExamAsync(examForAuth, teacherId);
            if (!authorized)
                return OperationResult.Failure("غير مصرح لك بتصحيح هذه المحاولة", 403);

            var exam = await _unitOfWork.Exams.GetWithQuestionsAsync(attempt.ExamId, CancellationToken.None);
            if (exam == null) return OperationResult.Failure("الامتحان غير موجود", 404);

            foreach (var ansDto in dto.Answers)
            {
                var answer = attempt.Answers.FirstOrDefault(a => a.Id == ansDto.AnswerId);
                if (answer == null) continue;

                var question = exam.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                // نسمح بتصحيح الأسئلة المقالية وأسئلة أكمل-الفراغ يدوياً
                if (question == null
                    || (question.QuestionType != Project.Domain.Enums.QuestionType.Essay
                        && question.QuestionType != Project.Domain.Enums.QuestionType.FillBlank)) continue;

                // منع إدخال درجة أعلى من الحد الأقصى
                var earned = Math.Min(ansDto.PointsEarned, question.Points);
                answer.PointsEarned = earned;
                answer.IsCorrect    = earned > 0;
                answer.AIFeedback   = ansDto.Feedback;   // يُستخدم كملاحظة المعلم
                _unitOfWork.StudentExamAnswers.Update(answer);
            }

            // إعادة حساب مجموع الدرجة
            attempt.Score = attempt.Answers.Sum(a => a.PointsEarned);

            // تحقق: هل لسه أسئلة مقالية/أكمل-فراغ بدون درجة محددة
            var hasUngradedManual = attempt.Answers
                .Join(exam.Questions, a => a.QuestionId, q => q.Id, (a, q) => new { a, q })
                .Any(x => (x.q.QuestionType == Project.Domain.Enums.QuestionType.Essay
                        || x.q.QuestionType == Project.Domain.Enums.QuestionType.FillBlank)
                       && !x.a.IsCorrect.HasValue);

            if (!hasUngradedManual)
                attempt.IsGraded = true;

            attempt.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.StudentExamAttempts.Update(attempt);
            await _unitOfWork.SaveChangesAsync(CancellationToken.None);

            return OperationResult.Success("تم حفظ التصحيح بنجاح");
        }

        /// <summary>
        /// التحقق من صلاحية المعلم على امتحان:
        ///   - CST موجود → المعلم صاحب الـ CST.
        ///   - CST=null (نشر للصف) → المعلم يُدرّس المادة (SubjectId).
        /// </summary>
        private async Task<bool> IsTeacherAuthorizedForExamAsync(Project.Domain.Entities.Exam exam, int teacherId)
        {
            // امتحان مربوط بفصل محدد
            if (exam.ClassSubjectTeacher is not null)
                return exam.ClassSubjectTeacher.TeacherId == teacherId;

            if (exam.ClassSubjectTeacherId.HasValue)
            {
                var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(exam.ClassSubjectTeacherId.Value);
                return cst is not null && !cst.IsDeleted && cst.TeacherId == teacherId;
            }

            // امتحان CST=null (نشر للصف كله) → المعلم لازم يُدرّس المادة
            if (exam.SubjectId.HasValue)
            {
                var csts = await _unitOfWork.ClassSubjectTeachers
                    .FindAsync(c => c.SubjectId == exam.SubjectId.Value
                                 && c.TeacherId == teacherId
                                 && !c.IsDeleted, CancellationToken.None);
                return csts.Count > 0;
            }

            return false;
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