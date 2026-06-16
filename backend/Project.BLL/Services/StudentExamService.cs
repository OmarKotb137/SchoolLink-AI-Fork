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

        var availability = ValidateExamAvailability(attempt.Exam);
        if (!availability.IsSuccess)
            return OperationResult<StudentExamAttemptResultDto>.Failure(availability.Message!, availability.StatusCode);

        var answersByQuestion = dto.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last());

        decimal totalScore = 0;
        var hasManualQuestions = false;

        foreach (var question in attempt.Exam.Questions.Where(q => !q.IsDeleted))
        {
            answersByQuestion.TryGetValue(question.Id, out var answerDto);
            var answer = new StudentExamAnswer
            {
                AttemptId = attempt.Id,
                QuestionId = question.Id,
                AnswerText = answerDto?.AnswerText,
                SelectedOptionId = answerDto?.SelectedOptionId,
                BooleanAnswer = answerDto?.BooleanAnswer
            };

            GradeObjectiveAnswer(question, answer);

            if (answer.IsCorrect.HasValue)
                totalScore += answer.PointsEarned;
            else
                hasManualQuestions = true;

            await _unitOfWork.StudentExamAnswers.AddAsync(answer);
        }

        attempt.SubmittedAt = DateTime.UtcNow;
        attempt.Score = totalScore;
        attempt.IsGraded = !hasManualQuestions;
        attempt.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.StudentExamAttempts.Update(attempt);
        await _unitOfWork.SaveChangesAsync();

        var updatedAttempt = await _unitOfWork.StudentExamAttempts.GetWithAnswersForEnrollmentAsync(attempt.Id, enrollmentResult.Data!.Id);
        return OperationResult<StudentExamAttemptResultDto>.Success(MapResult(updatedAttempt!, false), "تم تسليم الامتحان بنجاح");
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

        var now = DateTime.UtcNow;
        if (exam.StartTime.HasValue && now < exam.StartTime.Value)
            return OperationResult.Failure("الامتحان لم يبدأ بعد");

        if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            return OperationResult.Failure("انتهى وقت الامتحان");

        return OperationResult.Success();
    }

    private static string GetExamStatus(Exam exam, StudentExamAttempt? attempt, bool isResultPublished)
    {
        var now = DateTime.UtcNow;

        if (attempt?.SubmittedAt != null)
        {
            if (!attempt.IsGraded)
                return "submittedWaitingGrade";

            return isResultPublished ? "resultVisible" : "gradedHidden";
        }

        if (attempt != null)
            return "inProgress";

        if (exam.StartTime.HasValue && now < exam.StartTime.Value)
            return "upcoming";

        if (exam.EndTime.HasValue && now > exam.EndTime.Value)
            return "expired";

        return "available";
    }

    private static StudentExamAttemptStartedDto MapStartedAttempt(StudentExamAttempt attempt, Exam exam)
    {
        var endsAt = exam.DurationMinutes.HasValue
            ? attempt.StartedAt.AddMinutes(exam.DurationMinutes.Value)
            : exam.EndTime;

        return new StudentExamAttemptStartedDto
        {
            AttemptId = attempt.Id,
            ExamId = exam.Id,
            StartedAt = attempt.StartedAt,
            ServerNow = DateTime.UtcNow,
            DurationMinutes = exam.DurationMinutes,
            EndsAt = endsAt
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
                    .Select(a => new StudentExamResultAnswerDto
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.QuestionText,
                        AnswerText = a.AnswerText,
                        SelectedOptionId = a.SelectedOptionId,
                        BooleanAnswer = a.BooleanAnswer,
                        IsCorrect = a.IsCorrect,
                        PointsEarned = a.PointsEarned,
                        QuestionPoints = a.Question.Points,
                        AIFeedback = a.AIFeedback
                    }).ToList()
                : new List<StudentExamResultAnswerDto>()
        };
    }

    private static bool IsResultPublished(Exam exam)
    {
        // Result publishing for informal teacher exams will be added as a teacher-side feature.
        return false;
    }

    private static string GetSubjectName(Exam exam)
        => exam.Subject?.Name ?? exam.ClassSubjectTeacher?.Subject?.Name ?? string.Empty;
}
