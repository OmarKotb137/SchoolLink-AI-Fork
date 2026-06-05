using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Feedback;

namespace Project.BLL.Interfaces;

public interface ILessonFeedbackService
{
    Task<OperationResult<LessonFeedbackDto>> SubmitLessonFeedbackAsync(SubmitFeedbackRequest request);
    Task<OperationResult<FeedbackSummaryDto>> GetFeedbackSummaryByTeacherAsync(int classSubjectTeacherId, DateFilter filter);
    Task<OperationResult<IEnumerable<LessonFeedbackDto>>> GetFeedbackByTeacherRawAsync(int classSubjectTeacherId, DateFilter filter);
    Task<OperationResult<IEnumerable<LessonFeedbackDto>>> GetFeedbackByLessonDateAsync(int classSubjectTeacherId, DateOnly lessonDate);
    Task<OperationResult<IEnumerable<LessonFeedbackDto>>> GetFeedbackByEnrollmentAsync(int enrollmentId);
    Task<OperationResult<decimal>> GetOverallTeacherRatingAsync(int teacherId, int academicYearId);
    Task<OperationResult> DeleteFeedbackAsync(int feedbackId, int callerUserId);
    Task<OperationResult<LessonFeedbackDto>> UpdateFeedbackAsync(UpdateFeedbackRequest request);
}
