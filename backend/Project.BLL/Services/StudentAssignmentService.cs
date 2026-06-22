using Common.Results;
using Project.BLL.DTOs.StudentAssignments;
using Project.BLL.Interfaces;
using Project.BLL.Utils;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class StudentAssignmentService : IStudentAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentAssignmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<List<StudentAssignmentListItemDto>>> GetMyAssignmentsAsync(int userId, string? status = null, int? subjectId = null)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<List<StudentAssignmentListItemDto>>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var enrollment = enrollmentResult.Data!;
        var assignments = await _unitOfWork.Assignments.GetPublishedForEnrollmentAsync(enrollment.Id);
        var submissions = await _unitOfWork.StudentAssignmentSubmissions.GetByEnrollmentIdAsync(enrollment.Id);
        var submissionMap = submissions
            .Where(s => !s.IsDeleted)
            .GroupBy(s => s.AssignmentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.SubmittedAt).First());

        var result = assignments
            .Where(a => !subjectId.HasValue || a.ClassSubjectTeacher.SubjectId == subjectId.Value)
            .Select(a =>
            {
                submissionMap.TryGetValue(a.Id, out var submission);
                return MapListItem(a, submission);
            })
            .Where(a => string.IsNullOrWhiteSpace(status) || status == "all" || a.Status == status)
            .ToList();

        return OperationResult<List<StudentAssignmentListItemDto>>.Success(result, "تم جلب واجبات الطالب بنجاح");
    }

    public async Task<OperationResult<StudentAssignmentDetailsDto>> GetMyAssignmentDetailsAsync(int userId, int assignmentId)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentAssignmentDetailsDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var enrollment = enrollmentResult.Data!;
        var assignment = await _unitOfWork.Assignments.GetStudentAssignmentDetailsAsync(assignmentId, enrollment.Id);
        if (assignment == null || assignment.IsDeleted)
            return OperationResult<StudentAssignmentDetailsDto>.Failure("الواجب غير موجود أو غير متاح لهذا الطالب", 404);

        var submission = await _unitOfWork.StudentAssignmentSubmissions.GetByEnrollmentAndAssignmentAsync(enrollment.Id, assignmentId);
        var dto = new StudentAssignmentDetailsDto
        {
            AssignmentId = assignment.Id,
            Title = assignment.Title,
            Description = assignment.Description,
            SubjectName = GetSubjectName(assignment),
            ClassName = assignment.ClassSubjectTeacher?.Class?.Name ?? string.Empty,
            DueDate = assignment.DueDate,
            MaxScore = assignment.MaxScore,
            IsAutoGraded = assignment.IsAutoGraded,
            Status = GetAssignmentStatus(assignment, submission),
            SubmissionId = submission?.Id,
            Questions = assignment.Questions
                .Where(q => !q.IsDeleted)
                .OrderBy(q => q.DisplayOrder)
                .Select(q => new StudentAssignmentQuestionDto
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    ImageUrl = q.ImageUrl,
                    Points = q.Points,
                    DisplayOrder = q.DisplayOrder,
                    Options = q.Options
                        .Where(o => !o.IsDeleted)
                        .OrderBy(o => o.DisplayOrder)
                        .Select(o => new StudentAssignmentQuestionOptionDto
                        {
                            Id = o.Id,
                            OptionText = o.OptionText,
                            DisplayOrder = o.DisplayOrder
                        }).ToList()
                }).ToList()
        };

        return OperationResult<StudentAssignmentDetailsDto>.Success(dto, "تم جلب تفاصيل الواجب بنجاح");
    }

    public async Task<OperationResult<StudentAssignmentSubmissionResultDto>> SubmitAssignmentAsync(int userId, int assignmentId, SubmitStudentAssignmentDto dto)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentAssignmentSubmissionResultDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var enrollment = enrollmentResult.Data!;
        var assignment = await _unitOfWork.Assignments.GetStudentAssignmentDetailsAsync(assignmentId, enrollment.Id);
        if (assignment == null || assignment.IsDeleted)
            return OperationResult<StudentAssignmentSubmissionResultDto>.Failure("الواجب غير موجود أو غير متاح لهذا الطالب", 404);

        if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow.AddHours(3))
            return OperationResult<StudentAssignmentSubmissionResultDto>.Failure("انتهى موعد تسليم الواجب");

        var existingSubmission = await _unitOfWork.StudentAssignmentSubmissions.GetByEnrollmentAndAssignmentAsync(enrollment.Id, assignmentId);
        if (existingSubmission != null && !existingSubmission.IsDeleted)
            return OperationResult<StudentAssignmentSubmissionResultDto>.Failure("تم تسليم هذا الواجب مسبقا");

        var answersByQuestion = dto.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last());

        decimal totalScore = 0;
        var hasManualQuestions = false;

        var submission = new StudentAssignmentSubmission
        {
            EnrollmentId = enrollment.Id,
            AssignmentId = assignment.Id,
            SubmittedAt = DateTime.UtcNow,
            MaxScore = assignment.MaxScore,
            IsGraded = false
        };

        foreach (var question in assignment.Questions.Where(q => !q.IsDeleted).OrderBy(q => q.DisplayOrder))
        {
            answersByQuestion.TryGetValue(question.Id, out var answerDto);
            var answer = new StudentAssignmentAnswer
            {
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

            submission.Answers.Add(answer);
        }

        submission.Score = totalScore;
        submission.IsGraded = !hasManualQuestions;

        await _unitOfWork.StudentAssignmentSubmissions.AddAsync(submission);
        await _unitOfWork.SaveChangesAsync();

        var saved = await _unitOfWork.StudentAssignmentSubmissions.GetWithAnswersForEnrollmentAsync(submission.Id, enrollment.Id);
        return OperationResult<StudentAssignmentSubmissionResultDto>.Success(
            MapResult(saved!, saved!.IsGraded),
            saved.IsGraded ? "تم تسليم الواجب وتصحيحه بنجاح" : "تم تسليم الواجب بنجاح، في انتظار التصحيح");
    }

    public async Task<OperationResult<StudentAssignmentSubmissionResultDto>> GetSubmissionResultAsync(int userId, int submissionId)
    {
        var enrollmentResult = await GetCurrentEnrollmentAsync(userId);
        if (!enrollmentResult.IsSuccess)
            return OperationResult<StudentAssignmentSubmissionResultDto>.Failure(enrollmentResult.Message!, enrollmentResult.StatusCode);

        var submission = await _unitOfWork.StudentAssignmentSubmissions.GetWithAnswersForEnrollmentAsync(submissionId, enrollmentResult.Data!.Id);
        if (submission == null || submission.IsDeleted)
            return OperationResult<StudentAssignmentSubmissionResultDto>.Failure("التسليم غير موجود", 404);

        return OperationResult<StudentAssignmentSubmissionResultDto>.Success(MapResult(submission, submission.IsGraded));
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

    private static StudentAssignmentListItemDto MapListItem(Assignment assignment, StudentAssignmentSubmission? submission)
        => new()
        {
            AssignmentId = assignment.Id,
            Title = assignment.Title,
            SubjectName = GetSubjectName(assignment),
            ClassName = assignment.ClassSubjectTeacher?.Class?.Name ?? string.Empty,
            DueDate = assignment.DueDate,
            MaxScore = assignment.MaxScore,
            QuestionsCount = assignment.Questions.Count(q => !q.IsDeleted),
            Status = GetAssignmentStatus(assignment, submission),
            SubmissionId = submission?.Id,
            SubmittedAt = submission?.SubmittedAt,
            IsGraded = submission?.IsGraded ?? false,
            Score = submission?.IsGraded == true ? submission.Score : null
        };

    private static string GetAssignmentStatus(Assignment assignment, StudentAssignmentSubmission? submission)
    {
        if (submission != null && !submission.IsDeleted)
            return submission.IsGraded ? "graded" : "submittedWaitingGrade";

        if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow.AddHours(3))
            return "late";

        return "pending";
    }

    private static void GradeObjectiveAnswer(AssignmentQuestion question, StudentAssignmentAnswer answer)
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
            var correct = BooleanNormalizer.NormalizeBoolean(question.CorrectAnswer);
            var isCorrect = correct.HasValue && answer.BooleanAnswer.HasValue && correct.Value == answer.BooleanAnswer.Value;
            answer.IsCorrect = isCorrect;
            answer.PointsEarned = isCorrect ? question.Points : 0;
        }
    }

    private static StudentAssignmentSubmissionResultDto MapResult(StudentAssignmentSubmission submission, bool includeAnswers)
    {
        var message = !submission.IsGraded
            ? "تم التسليم، في انتظار تصحيح المعلم"
            : "تم تصحيح الواجب";

        return new StudentAssignmentSubmissionResultDto
        {
            SubmissionId = submission.Id,
            AssignmentId = submission.AssignmentId,
            IsSubmitted = true,
            IsGraded = submission.IsGraded,
            Score = submission.IsGraded ? submission.Score : null,
            MaxScore = submission.MaxScore,
            SubmittedAt = submission.SubmittedAt,
            Message = message,
            Answers = includeAnswers
                ? submission.Answers
                    .OrderBy(a => a.Question.DisplayOrder)
                    .Select(a => {
                        string? correctAnswerText = null;
                        if (a.Question.QuestionType == QuestionType.MultipleChoice)
                        {
                            var correctOpt = a.Question.Options.FirstOrDefault(o => o.IsCorrect && !o.IsDeleted);
                            if (correctOpt != null)
                                correctAnswerText = correctOpt.OptionText;
                        }
                        else if (a.Question.QuestionType == QuestionType.TrueFalse)
                        {
                            var normalized = BooleanNormalizer.NormalizeBoolean(a.Question.CorrectAnswer);
                            if (normalized.HasValue)
                                correctAnswerText = normalized.Value ? "صح" : "خطأ";
                            else
                                correctAnswerText = a.Question.CorrectAnswer;
                        }
                        else
                        {
                            correctAnswerText = a.Question.CorrectAnswer;
                        }

                        string? selectedOptionText = a.SelectedOptionId.HasValue
                            ? a.Question.Options.FirstOrDefault(o => o.Id == a.SelectedOptionId.Value && !o.IsDeleted)?.OptionText
                            : null;

                        return new StudentAssignmentResultAnswerDto
                        {
                            QuestionId = a.QuestionId,
                            QuestionText = a.Question.QuestionText,
                            AnswerText = a.AnswerText,
                            SelectedOptionId = a.SelectedOptionId,
                            SelectedOptionText = selectedOptionText,
                            BooleanAnswer = a.BooleanAnswer,
                            IsCorrect = a.IsCorrect,
                            PointsEarned = a.PointsEarned,
                            QuestionPoints = a.Question.Points,
                            CorrectAnswerText = correctAnswerText,
                            AIFeedback = a.AIFeedback
                        };
                    }).ToList()
                : new List<StudentAssignmentResultAnswerDto>()
        };
    }

    private static string GetSubjectName(Assignment assignment)
        => assignment.ClassSubjectTeacher?.Subject?.Name ?? string.Empty;
}
