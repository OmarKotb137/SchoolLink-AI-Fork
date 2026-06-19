using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Notifications;
using Project.BLL.DTOs.PeriodicAssessments;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class PeriodicAssessmentService : IPeriodicAssessmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly INotificationService _notificationService;

    public PeriodicAssessmentService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _notificationService = notificationService;
    }

    private static string GetAssessmentTypeName(PeriodicAssessmentType type) => type switch
    {
        PeriodicAssessmentType.MonthlyExam1 => "الامتحان الشهري الأول",
        PeriodicAssessmentType.MonthlyExam2 => "الامتحان الشهري الثاني",
        PeriodicAssessmentType.InitialAssessment => "التقييم التمهيدي",
        PeriodicAssessmentType.FinalAssessment => "التقييم النهائي",
        PeriodicAssessmentType.SemesterExam => "امتحان الفصل",
        _ => "التقييم الدوري"
    };

    public async Task<OperationResult<PeriodicAssessmentDto>> RecordPeriodicAssessmentAsync(
        RecordPeriodicAssessmentRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment is null || enrollment.IsDeleted || enrollment.LeftAt is not null)
            return OperationResult<PeriodicAssessmentDto>.Failure("القيد غير موجود أو غير نشط");

        var existing = await _unitOfWork.PeriodicAssessments.GetByEnrollmentAndTypeAsync(
            request.EnrollmentId, request.AssessmentType, request.Term, request.SubjectId);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<PeriodicAssessmentDto>.Failure("هذا التقييم مسجل مسبقاً لهذا الطالب");

        if (request.Score < 0 || request.Score > request.MaxScore)
            return OperationResult<PeriodicAssessmentDto>.Failure("الدرجة يجب أن تكون بين 0 و " + request.MaxScore);

        var entity = _mapper.Map<PeriodicAssessment>(request);

        await _unitOfWork.PeriodicAssessments.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        // إشعار لولي الأمر بتسجيل درجة امتحان الشهر
        await NotifyParentOfAssessmentAsync(entity, enrollment);

        return OperationResult<PeriodicAssessmentDto>.Success(
            _mapper.Map<PeriodicAssessmentDto>(entity),
            "تم تسجيل التقييم الدوري بنجاح");
    }

    public async Task<OperationResult<PeriodicAssessmentDto>> UpdatePeriodicAssessmentAsync(
        UpdatePeriodicAssessmentRequest request)
    {
        var entity = await _unitOfWork.PeriodicAssessments.GetByIdAsync(request.AssessmentId);
        if (entity is null || entity.IsDeleted)
            return OperationResult<PeriodicAssessmentDto>.Failure("التقييم الدوري غير موجود");

        if (request.Score < 0 || request.Score > entity.MaxScore)
            return OperationResult<PeriodicAssessmentDto>.Failure("الدرجة يجب أن تكون بين 0 و " + entity.MaxScore);

        var oldScore = entity.Score;
        entity.Score = request.Score;
        entity.AssessmentDate = request.AssessmentDate;

        _unitOfWork.PeriodicAssessments.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        // إشعار لولي أمر فقط لو الدرجة اتغيرت فعلاً
        if (oldScore != request.Score)
        {
            var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(entity.EnrollmentId);
            if (enrollment is not null)
                await NotifyParentOfAssessmentAsync(entity, enrollment);
        }

        return OperationResult<PeriodicAssessmentDto>.Success(
            _mapper.Map<PeriodicAssessmentDto>(entity),
            "تم تحديث التقييم الدوري بنجاح");
    }

    public async Task<OperationResult> DeletePeriodicAssessmentAsync(int id)
    {
        var entity = await _unitOfWork.PeriodicAssessments.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("التقييم الدوري غير موجود");

        _unitOfWork.PeriodicAssessments.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف التقييم الدوري بنجاح");
    }

    public async Task<OperationResult<IEnumerable<PeriodicAssessmentDto>>> GetByEnrollmentAsync(int enrollmentId, AcademicTerm? term = null)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<PeriodicAssessmentDto>>.Failure("القيد غير موجود");

        var assessments = await _unitOfWork.PeriodicAssessments.GetByEnrollmentIdAsync(enrollmentId);
        if (term.HasValue)
            assessments = assessments.Where(a => a.Term == term.Value).ToList();
        return OperationResult<IEnumerable<PeriodicAssessmentDto>>.Success(
            _mapper.Map<IEnumerable<PeriodicAssessmentDto>>(assessments),
            "تم جلب التقييمات الدورية بنجاح");
    }

    public async Task<OperationResult<PeriodicAssessmentDto>> GetPeriodicAssessmentByIdAsync(int id)
    {
        var entity = await _unitOfWork.PeriodicAssessments.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<PeriodicAssessmentDto>.Failure("التقييم الدوري غير موجود");

        return OperationResult<PeriodicAssessmentDto>.Success(
            _mapper.Map<PeriodicAssessmentDto>(entity),
            "تم جلب التقييم الدوري بنجاح");
    }

    public async Task<OperationResult<IEnumerable<PeriodicAssessmentDto>>> GetByClassAsync(int classId, AcademicTerm? term = null, int? subjectId = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<PeriodicAssessmentDto>>.Failure("الفصل غير موجود");

        var enrollments = await _unitOfWork.StudentEnrollments
            .GetActiveByClassAsync(classId, classEntity.AcademicYearId);

        if (!enrollments.Any())
            return OperationResult<IEnumerable<PeriodicAssessmentDto>>.Success(
                new List<PeriodicAssessmentDto>(), "لا يوجد طلاب في هذا الفصل");

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var allAssessments = new List<PeriodicAssessment>();

        foreach (var eid in enrollmentIds)
        {
            var assessments = await _unitOfWork.PeriodicAssessments.GetByEnrollmentIdAsync(eid);
            if (term.HasValue)
                assessments = assessments.Where(a => a.Term == term.Value).ToList();
            if (subjectId.HasValue)
                assessments = assessments.Where(a => a.SubjectId == subjectId.Value).ToList();
            allAssessments.AddRange(assessments);
        }

        return OperationResult<IEnumerable<PeriodicAssessmentDto>>.Success(
            _mapper.Map<IEnumerable<PeriodicAssessmentDto>>(allAssessments),
            "تم جلب التقييمات الدورية للفصل بنجاح");
    }

    /// <summary>إرسال إشعار لولي الأمر والطالب بتسجيل درجة امتحان شهري</summary>
    private async Task NotifyParentOfAssessmentAsync(PeriodicAssessment entity, StudentEnrollment enrollment)
    {
        try
        {
            var student = await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId);
            var studentName = student?.FullName ?? "طالب";
            var parentUsers = await _unitOfWork.ParentStudents
                .FindAsync(ps => ps.StudentId == enrollment.StudentId);
            var recipients = parentUsers.Select(ps => ps.ParentId).Distinct().ToList();
            if (student?.UserId != null)
                recipients.Add(student.UserId.Value);
            if (recipients.Count == 0) return;

            var examName = GetAssessmentTypeName(entity.AssessmentType);

            await _notificationService.SendBulkNotificationAsync(new SendBulkNotificationRequest
            {
                UserIds = recipients,
                Title = examName,
                Body = $"تم تسجيل درجة {examName} للطالب {studentName}: {entity.Score} من {entity.MaxScore}",
                Type = NotificationType.GradeAlert
            });
        }
        catch
        {
            // فشل الإشعار لا يوقف العملية
        }
    }
}
