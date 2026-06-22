using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.AssignmentSubmission;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;
using Project.BLL.DTOs.Notifications;

namespace Project.BLL.Services
{
    public class AssignmentSubmissionService : IAssignmentSubmissionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly INotificationService _notificationService;

        public AssignmentSubmissionService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _notificationService = notificationService;
        }

        public async Task<OperationResult<GetAssignmentSubmissionDto>> GetByIdAsync(int id)
        {
            var submission = await _unitOfWork.StudentAssignmentSubmissions.GetWithAnswersAsync(id);

            if (submission == null || submission.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("لم يتم العثور على التسليم", 404);

            var dto = _mapper.Map<GetAssignmentSubmissionDto>(submission);
            return OperationResult<GetAssignmentSubmissionDto>.Success(dto);
        }

        public async Task<OperationResult<List<AssignmentSubmissionSummaryDto>>> GetByAssignmentIdAsync(int assignmentId)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);

            if (assignment == null || assignment.IsDeleted)
                return OperationResult<List<AssignmentSubmissionSummaryDto>>.Failure("لم يتم العثور على الواجب", 404);

            var submissions = await _unitOfWork.StudentAssignmentSubmissions
                .GetByAssignmentIdAsync(assignmentId);

            var dtos = _mapper.Map<List<AssignmentSubmissionSummaryDto>>(submissions);
            return OperationResult<List<AssignmentSubmissionSummaryDto>>.Success(dtos);
        }

        public async Task<OperationResult<GetAssignmentSubmissionDto>> SubmitAsync(CreateAssignmentSubmissionDto dto)
        {
            var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(dto.EnrollmentId);
            if (enrollment == null || enrollment.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("لم يتم العثور على التسجيل", 404);

            var assignment = await _unitOfWork.Assignments.GetWithQuestionsAsync(dto.AssignmentId);
            if (assignment == null || assignment.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("لم يتم العثور على الواجب", 404);

            // DueDate مخزّن UTC حقيقي بعد إصلاح التخزين — نقارن UTC vs UTC مباشرة
            if (assignment.DueDate.HasValue && assignment.DueDate.Value < DateTime.UtcNow)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("لقد انتهى موعد تسليم الواجب");

            var existingSubmission = await _unitOfWork.StudentAssignmentSubmissions
                .GetByEnrollmentAndAssignmentAsync(dto.EnrollmentId, dto.AssignmentId);

            if (existingSubmission != null && !existingSubmission.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("تم تقديم الواجب مسبقاً");

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

            // Notify teacher about submission
            var assignmentCst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(assignment.ClassSubjectTeacherId);
            if (assignmentCst != null)
            {
                var subEnrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(dto.EnrollmentId);
                var studentName = subEnrollment?.Student?.FullName ?? "طالب";

                await _notificationService.SendNotificationAsync(new SendNotificationRequest
                {
                    UserId = assignmentCst.TeacherId,
                    Title = "تقديم واجب",
                    Body = $"قام الطالب {studentName} بتقديم الواجب: {assignment.Title}",
                    Type = NotificationType.HomeworkSubmitted
                });
            }

            var resultDto = _mapper.Map<GetAssignmentSubmissionDto>(submission);
            return OperationResult<GetAssignmentSubmissionDto>.Success(resultDto, "تم إنشاء التسليم بنجاح");
        }

        public async Task<OperationResult<List<AssignmentSubmissionSummaryDto>>> GetByStudentAsync(int enrollmentId)
        {
            var submissions = await _unitOfWork.StudentAssignmentSubmissions.GetByEnrollmentIdAsync(enrollmentId);
            var dtos = _mapper.Map<List<AssignmentSubmissionSummaryDto>>(submissions);
            return OperationResult<List<AssignmentSubmissionSummaryDto>>.Success(dtos);
        }

        public async Task<OperationResult> ReopenAsync(int submissionId)
        {
            var submission = await _unitOfWork.StudentAssignmentSubmissions.GetByIdAsync(submissionId);
            if (submission == null || submission.IsDeleted)
                return OperationResult.Failure("لم يتم العثور على التسليم", 404);

            if (!submission.IsGraded)
                return OperationResult.Failure("لم يتم تصحيح التسليم بعد");

            submission.IsGraded = false;
            submission.Score = null;
            submission.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.StudentAssignmentSubmissions.Update(submission);
            await _unitOfWork.SaveChangesAsync();

            return OperationResult.Success("تم إعادة فتح التسليم بنجاح");
        }

        public async Task<OperationResult> GradeAsync(int submissionId)
        {
            var submission = await _unitOfWork.StudentAssignmentSubmissions
                .GetWithAnswersAsync(submissionId);

            if (submission == null || submission.IsDeleted)
                return OperationResult.Failure("لم يتم العثور على التسليم");

            if (submission.IsGraded)
                return OperationResult.Failure("تم تصحيح التسليم مسبقاً");

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

            return OperationResult.Success("تم تصحيح التسليم بنجاح");
        }

        public async Task<OperationResult<GetAssignmentSubmissionDto>> GradeSubmissionAsync(GradeSubmissionRequest request)
        {
            var submission = await _unitOfWork.StudentAssignmentSubmissions
                .GetWithAnswersAsync(request.SubmissionId);

            if (submission == null || submission.IsDeleted)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("لم يتم العثور على التسليم", 404);

            if (submission.IsGraded)
                return OperationResult<GetAssignmentSubmissionDto>.Failure("تم تصحيح التسليم مسبقاً");

            foreach (var grade in request.AnswerGrades)
            {
                var answer = submission.Answers.FirstOrDefault(a => a.QuestionId == grade.QuestionId);
                if (answer == null) continue;

                answer.PointsEarned = grade.PointsEarned;
                answer.AIFeedback = grade.AiFeedback;
                answer.IsCorrect = grade.PointsEarned > 0;
                _unitOfWork.StudentAssignmentAnswers.Update(answer);
            }

            submission.Score = request.TotalScore;
            submission.IsGraded = true;
            submission.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.StudentAssignmentSubmissions.Update(submission);
            await _unitOfWork.SaveChangesAsync();

            // Notify student and parents about grading
            var subEnrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(submission.EnrollmentId);
            if (subEnrollment != null)
            {
                var recipients = new List<int>();
                var student = await _unitOfWork.Students.GetByIdAsync(subEnrollment.StudentId);
                if (student?.UserId != null)
                    recipients.Add(student.UserId.Value);

                var parentUsers = await _unitOfWork.ParentStudents
                    .FindAsync(ps => ps.StudentId == subEnrollment.StudentId);
                recipients.AddRange(parentUsers.Select(p => p.ParentId));

                if (recipients.Count != 0)
                {
                    await _notificationService.SendBulkNotificationAsync(new SendBulkNotificationRequest
                    {
                        UserIds = recipients.Distinct().ToList(),
                        Title = "تصحيح الواجب",
                        Body = $"تم تصحيح الواجب. الدرجة: {submission.Score} من {submission.MaxScore}",
                        Type = NotificationType.HomeworkGraded
                    });
                }
            }

            var dto = _mapper.Map<GetAssignmentSubmissionDto>(submission);
            return OperationResult<GetAssignmentSubmissionDto>.Success(dto, "تم تصحيح التسليم بنجاح");
        }
    }
}