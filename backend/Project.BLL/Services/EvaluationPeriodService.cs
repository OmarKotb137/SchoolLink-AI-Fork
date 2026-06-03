using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.EvaluationPeriods;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class EvaluationPeriodService : IEvaluationPeriodService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public EvaluationPeriodService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<EvaluationPeriodDto>> CreateEvaluationPeriodAsync(
        CreateEvaluationPeriodRequest request)
    {
        var year = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (year is null || year.IsDeleted)
            return OperationResult<EvaluationPeriodDto>.Failure("السنة الدراسية غير موجودة");

        var existing = await _unitOfWork.EvaluationPeriods.GetByAcademicYearAsync(request.AcademicYearId);
        if (existing.Any(p => p.Name == request.Name && !p.IsDeleted))
            return OperationResult<EvaluationPeriodDto>.Failure("اسم الفترة مكرر في نفس السنة الدراسية");

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate <= request.StartDate)
            return OperationResult<EvaluationPeriodDto>.Failure("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        var entity = _mapper.Map<EvaluationPeriod>(request);

        await _unitOfWork.EvaluationPeriods.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<EvaluationPeriodDto>.Success(
            _mapper.Map<EvaluationPeriodDto>(entity),
            "تم إنشاء فترة التقييم بنجاح");
    }

    public async Task<OperationResult<EvaluationPeriodDto>> UpdateEvaluationPeriodAsync(
        UpdateEvaluationPeriodRequest request)
    {
        var entity = await _unitOfWork.EvaluationPeriods.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<EvaluationPeriodDto>.Failure("فترة التقييم غير موجودة");

        var existing = await _unitOfWork.EvaluationPeriods.GetByAcademicYearAsync(entity.AcademicYearId);
        if (existing.Any(p => p.Id != request.Id && p.Name == request.Name && !p.IsDeleted))
            return OperationResult<EvaluationPeriodDto>.Failure("اسم الفترة مكرر في نفس السنة الدراسية");

        if (request.StartDate.HasValue && request.EndDate.HasValue && request.EndDate <= request.StartDate)
            return OperationResult<EvaluationPeriodDto>.Failure("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        entity.Name = request.Name;
        entity.PeriodType = request.PeriodType;
        entity.OrderNum = request.OrderNum;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.MonthName = request.MonthName;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvaluationPeriods.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<EvaluationPeriodDto>.Success(
            _mapper.Map<EvaluationPeriodDto>(entity),
            "تم تحديث فترة التقييم بنجاح");
    }

    public async Task<OperationResult> DeleteEvaluationPeriodAsync(int id)
    {
        var entity = await _unitOfWork.EvaluationPeriods.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("فترة التقييم غير موجودة");

        _unitOfWork.EvaluationPeriods.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف فترة التقييم بنجاح");
    }

    public async Task<OperationResult<EvaluationPeriodDto>> GetCurrentWeekAsync(int academicYearId)
    {
        var period = await _unitOfWork.EvaluationPeriods.GetCurrentWeekAsync(academicYearId);
        if (period is null)
            return OperationResult<EvaluationPeriodDto>.Failure("لا يوجد أسبوع تقييم حالي");

        return OperationResult<EvaluationPeriodDto>.Success(
            _mapper.Map<EvaluationPeriodDto>(period),
            "تم جلب الأسبوع الحالي بنجاح");
    }

    public async Task<OperationResult<IEnumerable<string>>> GetDistinctMonthNamesAsync(int academicYearId)
    {
        var months = await _unitOfWork.EvaluationPeriods.GetDistinctMonthNamesAsync(academicYearId);
        return OperationResult<IEnumerable<string>>.Success(months, "تم جلب أسماء الأشهر بنجاح");
    }

    public async Task<OperationResult<IEnumerable<EvaluationPeriodDto>>> GetPeriodsByMonthAsync(int academicYearId, string monthName)
    {
        var periods = await _unitOfWork.EvaluationPeriods.GetByMonthNameAsync(academicYearId, monthName);
        return OperationResult<IEnumerable<EvaluationPeriodDto>>.Success(
            _mapper.Map<IEnumerable<EvaluationPeriodDto>>(periods),
            "تم جلب فترات الشهر بنجاح");
    }

    public async Task<OperationResult<IEnumerable<EvaluationPeriodDto>>> GetPeriodsByAcademicYearAsync(
        int academicYearId, PeriodType? type = null)
    {
        var year = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId);
        if (year is null || year.IsDeleted)
            return OperationResult<IEnumerable<EvaluationPeriodDto>>.Failure("السنة الدراسية غير موجودة");

        IReadOnlyList<EvaluationPeriod> periods;
        if (type.HasValue)
            periods = await _unitOfWork.EvaluationPeriods.GetByTypeAndYearAsync(academicYearId, type.Value);
        else
            periods = await _unitOfWork.EvaluationPeriods.GetOrderedByYearAsync(academicYearId);

        return OperationResult<IEnumerable<EvaluationPeriodDto>>.Success(
            _mapper.Map<IEnumerable<EvaluationPeriodDto>>(periods),
            "تم جلب فترات التقييم بنجاح");
    }
}
