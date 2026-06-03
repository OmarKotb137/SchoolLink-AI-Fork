using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.StudentEvaluations;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class StudentEvaluationService : IStudentEvaluationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public StudentEvaluationService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<StudentEvaluationDto>> RecordEvaluationAsync(
        RecordEvaluationRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment is null || enrollment.IsDeleted || enrollment.LeftAt is not null)
            return OperationResult<StudentEvaluationDto>.Failure("القيد غير موجود أو غير نشط");

        var item = await _unitOfWork.EvaluationItems.GetByIdAsync(request.EvaluationItemId);
        if (item is null || item.IsDeleted || !item.IsVisible)
            return OperationResult<StudentEvaluationDto>.Failure("معيار التقييم غير موجود أو غير ظاهر");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(request.PeriodId);
        if (period is null || period.IsDeleted)
            return OperationResult<StudentEvaluationDto>.Failure("فترة التقييم غير موجودة");

        if (request.Score.HasValue && (request.Score < 0 || request.Score > item.MaxScore))
            return OperationResult<StudentEvaluationDto>.Failure("الدرجة يجب أن تكون بين 0 و " + item.MaxScore);

        var existing = await _unitOfWork.StudentEvaluations.GetByEnrollmentItemAndPeriodAsync(
            request.EnrollmentId, request.EvaluationItemId, request.PeriodId);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<StudentEvaluationDto>.Failure("يوجد تقييم مسجل بالفعل لهذا الطالب في هذا المعيار والفترة");

        var teacher = await _unitOfWork.Users.GetByIdAsync(request.EnteredById);
        if (teacher is null || teacher.IsDeleted || !teacher.IsActive)
            return OperationResult<StudentEvaluationDto>.Failure("المعلم غير موجود أو غير نشط");

        var entity = new StudentEvaluation
        {
            EnrollmentId = request.EnrollmentId,
            EvaluationItemId = request.EvaluationItemId,
            PeriodId = request.PeriodId,
            Score = request.Score,
            EnteredById = request.EnteredById,
            EnteredAt = DateTime.UtcNow
        };

        await _unitOfWork.StudentEvaluations.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<StudentEvaluationDto>.Success(
            _mapper.Map<StudentEvaluationDto>(entity),
            "تم تسجيل التقييم بنجاح");
    }

    public async Task<OperationResult<StudentEvaluationDto>> UpdateEvaluationAsync(
        UpdateEvaluationRequest request)
    {
        var entity = await _unitOfWork.StudentEvaluations.GetByIdAsync(request.EvaluationId);
        if (entity is null || entity.IsDeleted)
            return OperationResult<StudentEvaluationDto>.Failure("التقييم غير موجود");

        var item = await _unitOfWork.EvaluationItems.GetByIdAsync(entity.EvaluationItemId);
        if (item is null)
            return OperationResult<StudentEvaluationDto>.Failure("معيار التقييم غير موجود");

        if (request.NewScore.HasValue && (request.NewScore < 0 || request.NewScore > item.MaxScore))
            return OperationResult<StudentEvaluationDto>.Failure("الدرجة يجب أن تكون بين 0 و " + item.MaxScore);

        var teacher = await _unitOfWork.Users.GetByIdAsync(request.UpdatedById);
        if (teacher is null || teacher.IsDeleted || !teacher.IsActive)
            return OperationResult<StudentEvaluationDto>.Failure("المعلم غير موجود أو غير نشط");

        entity.Score = request.NewScore;
        entity.EnteredById = request.UpdatedById;
        entity.EnteredAt = DateTime.UtcNow;

        _unitOfWork.StudentEvaluations.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<StudentEvaluationDto>.Success(
            _mapper.Map<StudentEvaluationDto>(entity),
            "تم تحديث التقييم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<StudentEvaluationDto>>> GetByEnrollmentAndPeriodAsync(
        int enrollmentId, int periodId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<StudentEvaluationDto>>.Failure("القيد غير موجود");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period is null || period.IsDeleted)
            return OperationResult<IEnumerable<StudentEvaluationDto>>.Failure("فترة التقييم غير موجودة");

        var evaluations = await _unitOfWork.StudentEvaluations.GetByEnrollmentAndPeriodAsync(enrollmentId, periodId);
        return OperationResult<IEnumerable<StudentEvaluationDto>>.Success(
            _mapper.Map<IEnumerable<StudentEvaluationDto>>(evaluations),
            "تم جلب التقييمات بنجاح");
    }

    public async Task<OperationResult<IEnumerable<ClassEvaluationDto>>> GetByClassAndPeriodAsync(
        int classId, int periodId)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<ClassEvaluationDto>>.Failure("الفصل غير موجود");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period is null || period.IsDeleted)
            return OperationResult<IEnumerable<ClassEvaluationDto>>.Failure("فترة التقييم غير موجودة");

        var enrollments = await _unitOfWork.StudentEnrollments.GetActiveByClassAsync(classId, classEntity.AcademicYearId);
        var enrollmentIds = enrollments.Select(e => e.Id).ToList();

        var evaluations = await _unitOfWork.StudentEvaluations.GetByPeriodAndEnrollmentsAsync(periodId, enrollmentIds);
        var evaluationsByEnrollment = evaluations.GroupBy(e => e.EnrollmentId);

        var result = new List<ClassEvaluationDto>();
        foreach (var enrollment in enrollments)
        {
            var evals = evaluationsByEnrollment.FirstOrDefault(g => g.Key == enrollment.Id);
            result.Add(new ClassEvaluationDto
            {
                EnrollmentId = enrollment.Id,
                StudentName = enrollment.Student?.FullName ?? "",
                Evaluations = evals is not null
                    ? _mapper.Map<IEnumerable<StudentEvaluationDto>>(evals)
                    : new List<StudentEvaluationDto>()
            });
        }

        return OperationResult<IEnumerable<ClassEvaluationDto>>.Success(
            result.AsEnumerable(),
            "تم جلب درجات الفصل بنجاح");
    }
}
