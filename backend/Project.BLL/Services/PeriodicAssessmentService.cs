using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.PeriodicAssessments;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class PeriodicAssessmentService : IPeriodicAssessmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public PeriodicAssessmentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<PeriodicAssessmentDto>> RecordPeriodicAssessmentAsync(
        RecordPeriodicAssessmentRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment is null || enrollment.IsDeleted || enrollment.LeftAt is not null)
            return OperationResult<PeriodicAssessmentDto>.Failure("القيد غير موجود أو غير نشط");

        var existing = await _unitOfWork.PeriodicAssessments.GetByEnrollmentAndTypeAsync(
            request.EnrollmentId, request.AssessmentType);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<PeriodicAssessmentDto>.Failure("هذا التقييم مسجل مسبقاً لهذا الطالب");

        if (request.Score < 0 || request.Score > request.MaxScore)
            return OperationResult<PeriodicAssessmentDto>.Failure("الدرجة يجب أن تكون بين 0 و " + request.MaxScore);

        var entity = _mapper.Map<PeriodicAssessment>(request);

        await _unitOfWork.PeriodicAssessments.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

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

        entity.Score = request.Score;
        entity.AssessmentDate = request.AssessmentDate;

        _unitOfWork.PeriodicAssessments.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<PeriodicAssessmentDto>.Success(
            _mapper.Map<PeriodicAssessmentDto>(entity),
            "تم تحديث التقييم الدوري بنجاح");
    }

    public async Task<OperationResult<IEnumerable<PeriodicAssessmentDto>>> GetByEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<PeriodicAssessmentDto>>.Failure("القيد غير موجود");

        var assessments = await _unitOfWork.PeriodicAssessments.GetByEnrollmentIdAsync(enrollmentId);
        return OperationResult<IEnumerable<PeriodicAssessmentDto>>.Success(
            _mapper.Map<IEnumerable<PeriodicAssessmentDto>>(assessments),
            "تم جلب التقييمات الدورية بنجاح");
    }
}
