using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.PeriodAverages;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class PeriodAverageService : IPeriodAverageService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public PeriodAverageService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<PeriodAverageDto>> CalculateAndSaveAsync(
        CalculatePeriodAverageRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment is null || enrollment.IsDeleted || enrollment.LeftAt is not null)
            return OperationResult<PeriodAverageDto>.Failure("القيد غير موجود أو غير نشط");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(request.PeriodId);
        if (period is null || period.IsDeleted)
            return OperationResult<PeriodAverageDto>.Failure("فترة التقييم غير موجودة");

        var evaluations = await _unitOfWork.StudentEvaluations.GetByEnrollmentAndPeriodAsync(
            request.EnrollmentId, request.PeriodId);

        if (!evaluations.Any())
            return OperationResult<PeriodAverageDto>.Failure("لا توجد درجات مسجلة لهذه الفترة");

        var totalScore = evaluations.Sum(e => e.Score ?? 0);
        var itemIds = evaluations.Select(e => e.EvaluationItemId).Distinct().ToList();
        var evaluatedItems = await _unitOfWork.EvaluationItems
            .FindAsync(i => itemIds.Contains(i.Id));
        var totalMax = evaluatedItems
            .Where(i => i is not null && !i.IsDeleted)
            .Sum(i => i.MaxScore * i.Weight);

        var avgScore = totalMax > 0 ? (totalScore / totalMax) * 100 : 0;

        var existing = await _unitOfWork.PeriodAverages.GetByEnrollmentAndPeriodAsync(
            request.EnrollmentId, request.PeriodId);

        if (existing is not null && !existing.IsDeleted)
        {
            existing.AvgScore = avgScore;
            existing.MaxScore = totalMax;
            existing.CalculatedAt = DateTime.UtcNow;
            _unitOfWork.PeriodAverages.Update(existing);
        }
        else
        {
            existing = new PeriodAverage
            {
                EnrollmentId = request.EnrollmentId,
                PeriodId = request.PeriodId,
                AvgScore = avgScore,
                MaxScore = totalMax,
                CalculatedAt = DateTime.UtcNow
            };
            await _unitOfWork.PeriodAverages.AddAsync(existing);
        }

        await _unitOfWork.SaveChangesAsync();

        return OperationResult<PeriodAverageDto>.Success(
            _mapper.Map<PeriodAverageDto>(existing),
            "تم حساب وحفظ متوسط الفترة بنجاح");
    }

    public async Task<OperationResult<IEnumerable<PeriodAverageDto>>> GetByClassAndPeriodAsync(int classId, int periodId)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<PeriodAverageDto>>.Failure("الفصل غير موجود");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period is null || period.IsDeleted)
            return OperationResult<IEnumerable<PeriodAverageDto>>.Failure("فترة التقييم غير موجودة");

        var averages = await _unitOfWork.PeriodAverages.GetByClassAndPeriodAsync(classId, periodId);
        return OperationResult<IEnumerable<PeriodAverageDto>>.Success(
            _mapper.Map<IEnumerable<PeriodAverageDto>>(averages),
            "تم جلب متوسطات الفصل بنجاح");
    }

    public async Task<OperationResult> DeletePeriodAverageAsync(int id)
    {
        var entity = await _unitOfWork.PeriodAverages.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("متوسط الفترة غير موجود");

        _unitOfWork.PeriodAverages.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف متوسط الفترة بنجاح");
    }

    public async Task<OperationResult<IEnumerable<PeriodAverageDto>>> GetByEnrollmentAsync(int enrollmentId, AcademicTerm? term = null)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<PeriodAverageDto>>.Failure("القيد غير موجود");

        var averages = await _unitOfWork.PeriodAverages.GetByEnrollmentIdAsync(enrollmentId);
        if (term.HasValue)
        {
            var semesterNumber = (int)term.Value;
            var termPeriodIds = (await _unitOfWork.EvaluationPeriods
                .FindAsync(p => p.SemesterNumber == semesterNumber))
                .Select(p => p.Id)
                .ToHashSet();
            averages = averages.Where(a => termPeriodIds.Contains(a.PeriodId)).ToList();
        }
        return OperationResult<IEnumerable<PeriodAverageDto>>.Success(
            _mapper.Map<IEnumerable<PeriodAverageDto>>(averages),
            "تم جلب متوسطات الفترات بنجاح");
    }

    public async Task<OperationResult<PeriodAverageDto>> GetPeriodAverageByIdAsync(int id)
    {
        var entity = await _unitOfWork.PeriodAverages.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<PeriodAverageDto>.Failure("متوسط الفترة غير موجود");

        return OperationResult<PeriodAverageDto>.Success(
            _mapper.Map<PeriodAverageDto>(entity),
            "تم جلب متوسط الفترة بنجاح");
    }

    public async Task<OperationResult<int>> CalculateAllForClassAsync(int classId, int periodId)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<int>.Failure("الفصل غير موجود");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period is null || period.IsDeleted)
            return OperationResult<int>.Failure("فترة التقييم غير موجودة");

        var enrollments = await _unitOfWork.StudentEnrollments
            .GetActiveByClassAsync(classId, classEntity.AcademicYearId);

        if (!enrollments.Any())
            return OperationResult<int>.Success(0, "لا يوجد طلاب في هذا الفصل");

        var calculated = 0;
        foreach (var enrollment in enrollments)
        {
            var evaluations = await _unitOfWork.StudentEvaluations
                .GetByEnrollmentAndPeriodAsync(enrollment.Id, periodId);

            if (!evaluations.Any())
                continue;

            var totalScore = evaluations.Sum(e => e.Score ?? 0);
            var itemIds = evaluations.Select(e => e.EvaluationItemId).Distinct().ToList();
            var evaluatedItems = await _unitOfWork.EvaluationItems
                .FindAsync(i => itemIds.Contains(i.Id));
            var totalMax = evaluatedItems
                .Where(i => i is not null && !i.IsDeleted)
                .Sum(i => i.MaxScore * i.Weight);

            var avgScore = totalMax > 0 ? (totalScore / totalMax) * 100 : 0;

            var existing = await _unitOfWork.PeriodAverages
                .GetByEnrollmentAndPeriodAsync(enrollment.Id, periodId);

            if (existing is not null && !existing.IsDeleted)
            {
                existing.AvgScore = avgScore;
                existing.MaxScore = totalMax;
                existing.CalculatedAt = DateTime.UtcNow;
                _unitOfWork.PeriodAverages.Update(existing);
            }
            else
            {
                await _unitOfWork.PeriodAverages.AddAsync(new PeriodAverage
                {
                    EnrollmentId = enrollment.Id,
                    PeriodId = periodId,
                    AvgScore = avgScore,
                    MaxScore = totalMax,
                    CalculatedAt = DateTime.UtcNow
                });
            }
            calculated++;
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult<int>.Success(calculated, $"تم حساب متوسط {calculated} طالب بنجاح");
    }
}
