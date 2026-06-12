using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Feedback;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class LessonFeedbackService : ILessonFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public LessonFeedbackService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<LessonFeedbackDto>> SubmitLessonFeedbackAsync(SubmitFeedbackRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment == null || enrollment.IsDeleted || enrollment.LeftAt != null)
            return OperationResult<LessonFeedbackDto>.Failure("التسجيل غير موجود أو غير نشط");

        var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(request.ClassSubjectTeacherId);
        if (cst == null || cst.IsDeleted)
            return OperationResult<LessonFeedbackDto>.Failure("تعيين المادة للمعلم غير موجود");

        if (cst.ClassId != enrollment.ClassId)
            return OperationResult<LessonFeedbackDto>.Failure("تعيين المادة للمعلم لا ينتمي إلى فصل الطالب");

        if (request.LessonDate > DateOnly.FromDateTime(DateTime.UtcNow))
            return OperationResult<LessonFeedbackDto>.Failure("تاريخ الدرس لا يمكن أن يكون في المستقبل");

        if (request.Rating < 1 || request.Rating > 5)
            return OperationResult<LessonFeedbackDto>.Failure("التقييم يجب أن يكون بين 1 و 5");

        if (!Enum.IsDefined(typeof(LessonUnderstanding), request.Understanding))
            return OperationResult<LessonFeedbackDto>.Failure("مستوى الفهم غير صالح");

        var exists = await _unitOfWork.LessonFeedbacks.HasFeedbackAsync(
            request.EnrollmentId, request.ClassSubjectTeacherId, request.LessonDate);
        if (exists)
            return OperationResult<LessonFeedbackDto>.Failure("تم تقديم تقييم لهذا الدرس من قبل");

        var feedback = _mapper.Map<LessonFeedback>(request);
        await _unitOfWork.LessonFeedbacks.AddAsync(feedback);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<LessonFeedbackDto>(feedback);
        return OperationResult<LessonFeedbackDto>.Success(dto, "تم تسجيل تقييم الدرس بنجاح");
    }

    public async Task<OperationResult<FeedbackSummaryDto>> GetFeedbackSummaryByTeacherAsync(int classSubjectTeacherId, DateFilter filter)
    {
        var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(classSubjectTeacherId);
        if (cst == null || cst.IsDeleted)
            return OperationResult<FeedbackSummaryDto>.Failure("تعيين المادة للمعلم غير موجود");

        var allFeedback = await _unitOfWork.LessonFeedbacks.GetByDateRangeAsync(
            classSubjectTeacherId,
            filter.FromDate ?? DateOnly.MinValue,
            filter.ToDate ?? DateOnly.MaxValue);
        var feedbackList = allFeedback.Where(f => !f.IsDeleted).ToList();

        var summary = new FeedbackSummaryDto
        {
            TotalResponses = feedbackList.Count,
            AverageRating = feedbackList.Count > 0 ? Math.Round(feedbackList.Average(f => f.Rating), 1) : 0,
            UnderstandingBreakdown = new UnderstandingBreakdownDto
            {
                YesCount = feedbackList.Count(f => f.Understanding == LessonUnderstanding.Yes),
                PartialCount = feedbackList.Count(f => f.Understanding == LessonUnderstanding.Partial),
                NoCount = feedbackList.Count(f => f.Understanding == LessonUnderstanding.No)
            },
            RatingTrend = feedbackList
                .GroupBy(f => f.LessonDate)
                .Select(g => new RatingTrendDto
                {
                    Date = g.Key,
                    AverageRating = Math.Round(g.Average(f => f.Rating), 1),
                    ResponseCount = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToList()
        };

        return OperationResult<FeedbackSummaryDto>.Success(summary);
    }

    public async Task<OperationResult<IEnumerable<LessonFeedbackDto>>> GetFeedbackByEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<LessonFeedbackDto>>.Failure("التسجيل غير موجود");

        var feedback = await _unitOfWork.LessonFeedbacks.GetByEnrollmentIdAsync(enrollmentId);
        var ordered = feedback.Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.LessonDate);

        var dtos = _mapper.Map<IEnumerable<LessonFeedbackDto>>(ordered);
        return OperationResult<IEnumerable<LessonFeedbackDto>>.Success(dtos);
    }

    public async Task<OperationResult<IEnumerable<LessonFeedbackDto>>> GetFeedbackByTeacherRawAsync(int classSubjectTeacherId, DateFilter filter)
    {
        var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(classSubjectTeacherId);
        if (cst == null || cst.IsDeleted)
            return OperationResult<IEnumerable<LessonFeedbackDto>>.Failure("تعيين المادة للمعلم غير موجود");

        var allFeedback = await _unitOfWork.LessonFeedbacks.GetByDateRangeAsync(
            classSubjectTeacherId,
            filter.FromDate ?? DateOnly.MinValue,
            filter.ToDate ?? DateOnly.MaxValue);

        var ordered = allFeedback.Where(f => !f.IsDeleted)
            .OrderByDescending(f => f.LessonDate).ToList();

        var dtos = _mapper.Map<IEnumerable<LessonFeedbackDto>>(ordered);
        return OperationResult<IEnumerable<LessonFeedbackDto>>.Success(dtos);
    }

    public async Task<OperationResult<IEnumerable<LessonFeedbackDto>>> GetFeedbackByLessonDateAsync(int classSubjectTeacherId, DateOnly lessonDate)
    {
        var cst = await _unitOfWork.ClassSubjectTeachers.GetByIdAsync(classSubjectTeacherId);
        if (cst == null || cst.IsDeleted)
            return OperationResult<IEnumerable<LessonFeedbackDto>>.Failure("تعيين المادة للمعلم غير موجود");

        var feedback = await _unitOfWork.LessonFeedbacks.GetByLessonDateAsync(classSubjectTeacherId, lessonDate);
        var filtered = feedback.Where(f => !f.IsDeleted).ToList();

        var dtos = _mapper.Map<IEnumerable<LessonFeedbackDto>>(filtered);
        return OperationResult<IEnumerable<LessonFeedbackDto>>.Success(dtos);
    }

    public async Task<OperationResult<decimal>> GetOverallTeacherRatingAsync(int teacherId, int academicYearId)
    {
        var rating = await _unitOfWork.LessonFeedbacks.GetOverallTeacherRatingAsync(teacherId, academicYearId);
        return OperationResult<decimal>.Success(rating);
    }

    public async Task<OperationResult> DeleteFeedbackAsync(int feedbackId, int callerUserId)
    {
        var feedback = await _unitOfWork.LessonFeedbacks.GetByIdAsync(feedbackId);
        if (feedback == null || feedback.IsDeleted)
            return OperationResult.Failure("التقييم غير موجود");

        var caller = await _unitOfWork.Users.GetByIdAsync(callerUserId);
        if (caller == null || caller.IsDeleted)
            return OperationResult.Failure("المستخدم غير موجود");

        if (!caller.Role.IsAdminLike())
            return OperationResult.Failure("فقط المدراء يمكنهم حذف التقييمات");

        _unitOfWork.LessonFeedbacks.SoftDelete(feedback);
        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم حذف التقييم بنجاح");
    }

    public async Task<OperationResult<LessonFeedbackDto>> UpdateFeedbackAsync(UpdateFeedbackRequest request)
    {
        var feedback = await _unitOfWork.LessonFeedbacks.GetByIdAsync(request.Id);
        if (feedback == null || feedback.IsDeleted)
            return OperationResult<LessonFeedbackDto>.Failure("التقييم غير موجود");

        if (request.Rating < 1 || request.Rating > 5)
            return OperationResult<LessonFeedbackDto>.Failure("التقييم يجب أن يكون بين 1 و 5");

        feedback.Rating = request.Rating;
        feedback.Understanding = request.Understanding;
        feedback.Comment = request.Comment;
        feedback.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.LessonFeedbacks.Update(feedback);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<LessonFeedbackDto>(feedback);
        return OperationResult<LessonFeedbackDto>.Success(dto, "تم تحديث التقييم بنجاح");
    }
}
