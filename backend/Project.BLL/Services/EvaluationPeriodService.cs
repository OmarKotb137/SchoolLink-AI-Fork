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
