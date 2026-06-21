using Common.Results;
using Project.BLL.DTOs.StudentExams;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class StudentExamService : IStudentExamService
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentExamService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<List<StudentExamListItemDto>>> GetMyExamsAsync(int userId)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<List<StudentExamListItemDto>>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var enrollment = enrollmentResult.Data!;
        var exams = await _unitOfWork.Exams.GetPublishedForEnrollmentAsync(enrollment.Id);
        var attempts = await _unitOfWork.StudentExamAttempts.GetByEnrollmentIdAsync(enrollment.Id);

        var result = exams.Select(exam =>
        {
            var latestAttempt = attempts
                .Where(a => a.ExamId == exam.Id && !a.IsDeleted)
                .OrderByDescending(a => a.StartedAt)
                .FirstOrDefault();

            var isResultPublished = IsResultPublished(exam);

            return new StudentExamListItemDto
            {
                ExamId = exam.Id,
                Title = exam.Title,
                SubjectName = GetSubjectName(exam),
                StartTime = exam.StartTime,
                EndTime = exam.EndTime,
                DurationMinutes = exam.DurationMinutes,
                TotalScore = exam.TotalScore,
                QuestionsCount = exam.Questions.Count(q => !q.IsDeleted),
                Status = GetExamStatus(exam, latestAttempt, isResultPublished),
                AttemptId = latestAttempt?.Id,
                StartedAt = latestAttempt?.StartedAt,
                SubmittedAt = latestAttempt?.SubmittedAt,
                IsGraded = latestAttempt?.IsGraded ?? false,
                IsResultPublished = isResultPublished,
                Score = isResultPublished ? latestAttempt?.Score : null
            };
        }).ToList();

        return OperationResult<List<StudentExamListItemDto>>.Success(result, "تم جلب امتحانات الطالب بنجاح");
    }

    public async Task<OperationResult<StudentExamDetailsDto>> GetMyExamDetailsAsync(int userId, int examId)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentExamDetailsDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var exam = await _unitOfWork.Exams.GetStudentExamDetailsAsync(examId, enrollmentResult.Data!.Id);
        if (exam == null || exam.IsDeleted)
            return OperationResult<StudentExamDetailsDto>.Failure("الامتحان غير موجود أو غير متاح لهذا الطالب", 404);

        var activeAttempt = await _unitOfWork.StudentExamAttempts.GetActiveAttemptAsync(enrollmentResult.Data!.Id, examId);

        var dto = new StudentExamDetailsDto
        {
            ExamId = exam.Id,
            Title = exam.Title,
            SubjectName = GetSubjectName(exam),
            ClassName = exam.ClassSubjectTeacher?.Class?.Name ?? string.Empty,
            StartTime = exam.StartTime,
            EndTime = exam.EndTime,
            DurationMinutes = exam.DurationMinutes,
            TotalScore = exam.TotalScore,
            Status = GetExamStatus(exam, activeAttempt, false),
            Questions = exam.Questions
                .Where(q => !q.IsDeleted)
                .OrderBy(q => q.DisplayOrder)
                .Select(q => new StudentExamQuestionDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    ContentText = q.ContentText,
                    ImageUrl = q.ImageUrl,
                    Points = q.Points,
                    DisplayOrder = q.DisplayOrder,
                    Options = q.Options
                        .Where(o => !o.IsDeleted)
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => new StudentExamQuestionOptionDto
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            DisplayOrder = o.DisplayOrder
                        }).ToList()
                }).ToList()
        };

        return OperationResult<StudentExamDetailsDto>.Success(dto, "تم جلب تفاصيل الامتحان بنجاح");
    }

    public async Task<OperationResult<StudentExamAttemptStartedDto>> StartOrResumeAttemptAsync(int userId, int examId)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentExamAttemptStartedDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var enrollment = enrollmentResult.Data!;
        var exam = await _unitOfWork.Exams.GetStudentExamDetailsAsync(examId, enrollment.Id);
        if (exam == null || exam.IsDeleted)
            return OperationResult<StudentExamAttemptStartedDto>.Failure("الامتحان غير موجود أو غير متاح لهذا الطالب", 404);

        var availability = ValidateExamAvailability(exam);
        if (!availability.IsSuccess)
            return OperationResult<StudentExamAttemptStartedDto>.Failure(availability.Message!, availability.StatusCode);

        var activeAttempt = await _unitOfWork.StudentExamAttempts.GetActiveAttemptAsync(enrollment.Id, examId);
        if (activeAttempt != null && !activeAttempt.IsDeleted)
            return OperationResult<StudentExamAttemptStartedDto>.Success(MapStartedAttempt(activeAttempt, exam), "تم استكمال المحاولة");

        var attempt = new StudentExamAttempt
        {
            EnrollmentId = enrollment.Id,
            ExamId = exam.Id,
            StartedAt = DateTime.UtcNow,
            TotalScore = exam.TotalScore
        };

        await _unitOfWork.StudentExamAttempts.AddAsync(attempt);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<StudentExamAttemptStartedDto>.Success(MapStartedAttempt(attempt, exam), "تم بدء الامتحان بنجاح");
    }

    public async Task<OperationResult<StudentExamAttemptStartedDto>> GetActiveAttemptAsync(int userId, int examId)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentExamAttemptStartedDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var exam = await _unitOfWork.Exams.GetStudentExamDetailsAsync(examId, enrollmentResult.Data!.Id);
        if (exam == null || exam.IsDeleted)
            return OperationResult<StudentExamAttemptStartedDto>.Failure("الامتحان غير موجود أو غير متاح لهذا الطالب", 404);

        var attempt = await _unitOfWork.StudentExamAttempts.GetActiveAttemptAsync(enrollmentResult.Data!.Id, examId);
        if (attempt == null || attempt.IsDeleted)
            return OperationResult<StudentExamAttemptStartedDto>.Failure("لا توجد محاولة مفتوحة لهذا الامتحان", 404);

        return OperationResult<StudentExamAttemptStartedDto>.Success(MapStartedAttempt(attempt, exam));
    }

    public async Task<OperationResult<StudentExamAttemptResultDto>> SubmitAttemptAsync(int userId, int attemptId, SubmitStudentExamAttemptDto dto)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentExamAttemptResultDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var attempt = await _unitOfWork.StudentExamAttempts.GetWithAnswersForEnrollmentAsync(attemptId, enrollmentResult.Data!.Id);
        if (attempt == null || attempt.IsDeleted)
            return OperationResult<StudentExamAttemptResultDto>.Failure("المحاولة غير موجودة", 404);

        if (attempt.SubmittedAt.HasValue)
            return OperationResult<StudentExamAttemptResultDto>.Failure("تم تسليم هذه المحاولة بالفعل");

        // سجل وقت التسليم فوراً وقم بحفظه قبل البدء في عملية التصحيح الثقيلة
        // هذا يضمن أن حالة 'تم التسليم' تُسجَّل حتى لو فشلت مرحلة التصحيح لاحقاً أو استغرقت وقتاً طويلاً
        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.StudentExamAttempts.Update(attempt);
        await _unitOfWork.SaveChangesAsync();

        // الإجابات معتمدة من الـ DB (محفوظة مسبقاً عن طريق الـ Auto-Save) مش من الـ DTO القادم في الطلب
        var savedAnswersByQuestion = attempt.Answers.ToDictionary(a => a.QuestionId);

        decimal totalScore = 0;
        var hasManualQuestions = false;

        foreach (var question in attempt.Exam.Questions.Where(q => !q.IsDeleted))
        {
            if (savedAnswersByQuestion.TryGetValue(question.Id, out var existingAnswer))
            {
                GradeObjectiveAnswer(question, existingAnswer);

                if (existingAnswer.IsCorrect.HasValue)
                    totalScore += existingAnswer.PointsEarned;
                else
                    hasManualQuestions = true;

                _unitOfWork.StudentExamAnswers.Update(existingAnswer);
            }
            else
            {
                // الطالب ملوش إجابة محفوظة لهذا السؤال — يُسجَّل بدون إجابة (null)
                var answer = new StudentExamAnswer
                {
                    AttemptId = attempt.Id,
                    QuestionId = question.Id,
                    AnswerText = null,
                    SelectedOptionId = null,
                    BooleanAnswer = null
                };

                GradeObjectiveAnswer(question, answer);

                if (answer.IsCorrect.HasValue)
                    totalScore += answer.PointsEarned;
                else
                    hasManualQuestions = true;

                await _unitOfWork.StudentExamAnswers.AddAsync(answer);
            }
        }

        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Score = totalScore;
        attempt.IsGraded = !hasManualQuestions;
        attempt.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.StudentExamAttempts.Update(attempt);
        await _unitOfWork.SaveChangesAsync();

        // ✅ نستخدم الـ object اللي في الميموري مباشرة بدل من إعادة جلب query ثقيلة ثانية من الـ DB
        return OperationResult<StudentExamAttemptResultDto>.Success(MapResult(attempt, false), "تم تسليم الامتحان بنجاح");
    }

    public async Task<OperationResult<StudentExamAttemptResultDto>> GetAttemptResultAsync(int userId, int attemptId)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentExamAttemptResultDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var attempt = await _unitOfWork.StudentExamAttempts.GetWithAnswersForEnrollmentAsync(attemptId, enrollmentResult.Data!.Id);
        if (attempt == null || attempt.IsDeleted)
            return OperationResult<StudentExamAttemptResultDto>.Failure("المحاولة غير موجودة", 404);

        return OperationResult<StudentExamAttemptResultDto>.Success(MapResult(attempt, IsResultPublished(attempt.Exam)));
    }

    public async Task<OperationResult> SaveAnswerProgressAsync(int userId, int attemptId, SaveAnswerProgressDto dto)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var attempt = await _unitOfWork.StudentExamAttempts.GetByIdAsync(attemptId);
        if (attempt == null || attempt.IsDeleted || attempt.EnrollmentId != enrollmentResult.Data!.Id)
            return OperationResult.Failure("المحاولة غير موجودة", 404);

        if (attempt.SubmittedAt.HasValue)
            return OperationResult.Failure("تم تسليم هذه المحاولة بالفعل", 409);

        var existingAnswer = await _unitOfWork.StudentExamAnswers.GetByAttemptAndQuestionAsync(attemptId, dto.QuestionId);
        if (existingAnswer != null)
        {
            existingAnswer.AnswerText = dto.AnswerText;
            existingAnswer.SelectedOptionId = dto.SelectedOptionId;
            existingAnswer.BooleanAnswer = dto.BooleanAnswer;
            _unitOfWork.StudentExamAnswers.Update(existingAnswer);
        }
        else
        {
            var answer = new StudentExamAnswer
            {
                AttemptId = attemptId,
                QuestionId = dto.QuestionId,
                AnswerText = dto.AnswerText,
                SelectedOptionId = dto.SelectedOptionId,
                BooleanAnswer = dto.BooleanAnswer
            };
            await _unitOfWork.StudentExamAnswers.AddAsync(answer);
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم تحديث الإجابة بنجاح");
    }

    private async Task<OperationResult<StudentEnrollment>> GetCurrentEnrollmentAsync(int userId)
    {
        var student = await _unitOfWork.Students.GetByUserIdAsync(userId);
        if (student == null || student.IsDeleted || !student.IsActive)
            return OperationResult<StudentEnrollment>.Failure("لم يتم العثور على الطالب", 404);

        var currentYear = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (currentYear == null || currentYear.IsDeleted)
            return OperationResult<StudentEnrollment>.Failure("لا توجد سنة دراسية حالية", 404);

        var enrollment = await _unitOfWork.StudentEnrollments.GetActiveByStudentAndYearAsync(student.Id, currentYear.Id);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<StudentEnrollment>.Failure("لا يوجد تسجيل حالي للطالب", 404);

        return OperationResult<StudentEnrollment>.Success(enrollment);
    }

    private static OperationResult ValidateExamAvailability(Exam exam)
    {
        if (!exam.IsPublished)
            return OperationResult.Failure("الامتحان غير منشور");

        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
        if (exam.StartTime.HasValue && now < exam.StartTime.Value)
            return OperationResult.Failure("الامتحان لم يبدأ بعد");

        if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            return OperationResult.Failure("انتهى وقت الامتحان");

        return OperationResult.Success();
    }

    private static string GetExamStatus(Exam exam, StudentExamAttempt? attempt, bool isResultPublished)
    {
        var now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

        if (attempt?.SubmittedAt != null)
        {
            if (!attempt.IsGraded)
                return "submittedWaitingGrade";

            return isResultPublished ? "resultVisible" : "gradedHidden";
        }

        if (attempt != null)
        {
            var isTimeUp = false;
            if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            {
                isTimeUp = true;
            }
            else if (exam.DurationMinutes.HasValue)
            {
                var startedAtUtc = DateTime.SpecifyKind(attempt.StartedAt, DateTimeKind.Utc);
                if (DateTime.UtcNow > startedAtUtc.AddMinutes(exam.DurationMinutes.Value))
                {
                    isTimeUp = true;
                }
            }

            if (isTimeUp)
            {
                return "expired";
            }

            return "inProgress";
        }

        if (exam.StartTime.HasValue && now < exam.StartTime.Value)
            return "upcoming";

        if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            return "expired";

        return "available";
    }

    private static StudentExamAttemptStartedDto MapStartedAttempt(StudentExamAttempt attempt, Exam exam)
    {
        // Ensure StartedAt has Utc kind so it serializes with 'Z'
        var startedAtUtc = DateTime.SpecifyKind(attempt.StartedAt, DateTimeKind.Utc);
        
        DateTime? endsAtUtc = null;
        if (exam.DurationMinutes.HasValue)
        {
            endsAtUtc = startedAtUtc.AddMinutes(exam.DurationMinutes.Value);
        }

        // The exam.EndTime is stored as Egypt Local Time (UTC+3). Convert it to UTC for absolute comparison.
        if (exam.EndTime.HasValue)
        {
            var examEndUtc = DateTime.SpecifyKind(exam.EndTime.Value.AddHours(-3), DateTimeKind.Utc);
            if (endsAtUtc == null || examEndUtc < endsAtUtc.Value)
            {
                endsAtUtc = examEndUtc;
            }
        }

        return new StudentExamAttemptStartedDto
        {
            AttemptId = attempt.Id,
            ExamId = exam.Id,
            StartedAt = startedAtUtc,
            ServerNow = DateTime.UtcNow,
            DurationMinutes = exam.DurationMinutes,
            EndsAt = endsAtUtc
        };
    }

    private static void GradeObjectiveAnswer(ExamQuestion question, StudentExamAnswer answer)
    {
        if (question.QuestionType == QuestionType.MultipleChoice)
        {
            var selectedOption = question.Options.FirstOrDefault(o => o.Id == answer.SelectedOptionId && !o.IsDeleted);
            var isCorrect = selectedOption?.IsCorrect == true;
            answer.IsCorrect = isCorrect;
            answer.PointsEarned = isCorrect ? question.Points : 0;
            return;
        }

        if (question.QuestionType == QuestionType.TrueFalse)
        {
            var correct = NormalizeBoolean(question.CorrectAnswer);
            var isCorrect = correct.HasValue && answer.BooleanAnswer.HasValue && correct.Value == answer.BooleanAnswer.Value;
            answer.IsCorrect = isCorrect;
            answer.PointsEarned = isCorrect ? question.Points : 0;
        }
    }

    private static bool? NormalizeBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "صح" or "صحيح" or "1" => true,
            "false" or "خطأ" or "خطا" or "0" => false,
            _ => null
        };
    }

    private static StudentExamAttemptResultDto MapResult(StudentExamAttempt attempt, bool isResultPublished)
    {
        var message = !attempt.SubmittedAt.HasValue
            ? "لم يتم تسليم الامتحان بعد"
            : !attempt.IsGraded
                ? "تم التسليم، في انتظار التصحيح"
                : !isResultPublished
                    ? "تم التصحيح، لكن المعلم لم ينشر النتيجة بعد"
                    : "تم إعلان النتيجة";

        return new StudentExamAttemptResultDto
        {
            AttemptId = attempt.Id,
            IsSubmitted = attempt.SubmittedAt.HasValue,
            IsGraded = attempt.IsGraded,
            IsResultPublished = isResultPublished,
            Score = isResultPublished ? attempt.Score : null,
            TotalScore = attempt.TotalScore,
            Message = message,
            Answers = isResultPublished
                ? attempt.Answers
                    .OrderBy(a => a.Question.DisplayOrder)
                    .Select(a => {
                        string? finalAnswerText = a.AnswerText;
                        string? correctAnswerText = null;

                        if (a.Question.QuestionType == QuestionType.MultipleChoice)
                        {
                            var selectedOpt = a.Question.Options.FirstOrDefault(o => o.Id == a.SelectedOptionId);
                            if (selectedOpt != null)
                            {
                                finalAnswerText = selectedOpt.OptionText;
                            }
                            var correctOpt = a.Question.Options.FirstOrDefault(o => o.IsCorrect);
                            if (correctOpt != null)
                            {
                                correctAnswerText = correctOpt.OptionText;
                            }
                        }
                        else if (a.Question.QuestionType == QuestionType.TrueFalse)
                        {
                            if (a.BooleanAnswer.HasValue)
                            {
                                finalAnswerText = a.BooleanAnswer.Value ? "صح" : "خطأ";
                            }
                            
                            var normalizedCorrect = NormalizeBoolean(a.Question.CorrectAnswer);
                            if (normalizedCorrect.HasValue)
                            {
                                correctAnswerText = normalizedCorrect.Value ? "صح" : "خطأ";
                            }
                            else
                            {
                                correctAnswerText = a.Question.CorrectAnswer;
                            }
                        }
                        else if (a.Question.QuestionType == QuestionType.Essay)
                        {
                            correctAnswerText = a.Question.CorrectAnswer;
                        }

                        return new StudentExamResultAnswerDto
                        {
                            QuestionId = a.QuestionId,
                            QuestionText = a.Question.QuestionText,
                            AnswerText = finalAnswerText,
                            SelectedOptionId = a.SelectedOptionId,
                            BooleanAnswer = a.BooleanAnswer,
                            IsCorrect = a.IsCorrect,
                            CorrectAnswerText = correctAnswerText,
                            PointsEarned = a.PointsEarned,
                            QuestionPoints = a.Question.Points,
                            AIFeedback = a.AIFeedback
                        };
                    }).ToList()
                : new List<StudentExamResultAnswerDto>()
        };
    }

    private static bool IsResultPublished(Exam exam)
    {
        return exam.IsResultPublished;
    }

    private static string GetSubjectName(Exam exam)
        => exam.Subject?.Name ?? exam.ClassSubjectTeacher?.Subject?.Name ?? string.Empty;
}
