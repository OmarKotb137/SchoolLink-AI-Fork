using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class AcademicYearService : IAcademicYearService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public AcademicYearService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<AcademicYearDto>> CreateAcademicYearAsync(
        CreateAcademicYearRequest request)
    {
        // 1. Name uniqueness
        if (await _unitOfWork.AcademicYears.ExistsByNameAsync(request.Name))
            return OperationResult<AcademicYearDto>.Failure("اسم السنة الدراسية موجود بالفعل");

        // 2. Date consistency
        if (request.EndDate <= request.StartDate)
            return OperationResult<AcademicYearDto>.Failure("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        // 3. Overlap check
        var all = await _unitOfWork.AcademicYears.GetAllOrderedByStartDateAsync();
        if (all.Any(y => y.StartDate < request.EndDate && y.EndDate > request.StartDate))
            return OperationResult<AcademicYearDto>.Failure(
                "الفترة الزمنية تتداخل مع سنة دراسية موجودة");

        // 4. Create entity
        var entity = _mapper.Map<AcademicYear>(request);
        entity.IsCurrent = false;

        // 5. Persist
        await _unitOfWork.AcademicYears.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<AcademicYearDto>.Success(
            _mapper.Map<AcademicYearDto>(entity),
            "تم إنشاء السنة الدراسية بنجاح");
    }

    public async Task<OperationResult<AcademicYearDto>> UpdateAcademicYearAsync(
        UpdateAcademicYearRequest request)
    {
        // 1. Find entity
        var entity = await _unitOfWork.AcademicYears.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<AcademicYearDto>.Failure("السنة الدراسية غير موجودة");

        // 2. Name uniqueness excluding current entity
        var existing = await _unitOfWork.AcademicYears.GetByNameAsync(request.Name);
        if (existing is not null && existing.Id != request.Id)
            return OperationResult<AcademicYearDto>.Failure("اسم السنة الدراسية مستخدم بالفعل");

        // 3. Date consistency
        if (request.EndDate <= request.StartDate)
            return OperationResult<AcademicYearDto>.Failure("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        // 4. Overlap check excluding current entity
        var years = await _unitOfWork.AcademicYears.GetAllOrderedByStartDateAsync();
        if (years.Any(y =>
                y.Id != request.Id &&
                y.StartDate < request.EndDate &&
                y.EndDate > request.StartDate))
            return OperationResult<AcademicYearDto>.Failure(
                "الفترة الزمنية تتداخل مع سنة دراسية موجودة");

        // 5. Apply updates
        entity.Name      = request.Name;
        entity.StartDate = request.StartDate;
        entity.EndDate   = request.EndDate;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AcademicYears.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<AcademicYearDto>.Success(
            _mapper.Map<AcademicYearDto>(entity),
            "تم تحديث السنة الدراسية بنجاح");
    }

    public async Task<OperationResult> DeleteAcademicYearAsync(int id)
    {
        // Academic years are protected once operational data depends on them.
        var entity = await _unitOfWork.AcademicYears.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("السنة الدراسية غير موجودة");

        if (entity.IsCurrent)
            return OperationResult.Failure("لا يمكن حذف السنة الدراسية الحالية");

        if (await _unitOfWork.Classes.AnyAsync(c => c.AcademicYearId == id) ||
            await _unitOfWork.StudentEnrollments.AnyAsync(e => e.AcademicYearId == id) ||
            await _unitOfWork.ClassSubjectTeachers.AnyAsync(cst => cst.AcademicYearId == id) ||
            await _unitOfWork.Timetables.AnyAsync(t => t.AcademicYearId == id) ||
            await _unitOfWork.EvaluationTemplates.AnyAsync(t => t.AcademicYearId == id) ||
            await _unitOfWork.ResultVisibilitySettings.AnyAsync(s => s.AcademicYearId == id))
            return OperationResult.Failure("لا يمكن حذف سنة دراسية مستخدمة في بيانات أخرى");

        _unitOfWork.AcademicYears.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف السنة الدراسية بنجاح");
    }

    public async Task<OperationResult<AcademicYearDto>> GetAcademicYearByIdAsync(int id)
    {
        var entity = await _unitOfWork.AcademicYears.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<AcademicYearDto>.Failure("السنة الدراسية غير موجودة");

        return OperationResult<AcademicYearDto>.Success(
            _mapper.Map<AcademicYearDto>(entity),
            "تم جلب السنة الدراسية بنجاح");
    }

    public async Task<OperationResult> SetCurrentAcademicYearAsync(int id)
    {
        // 1. Find year
        var year = await _unitOfWork.AcademicYears.GetByIdAsync(id);
        if (year is null || year.IsDeleted)
            return OperationResult.Failure("السنة الدراسية غير موجودة");

        // 2. Transaction: unset all, then set this one
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var all = await _unitOfWork.AcademicYears.GetAllOrderedByStartDateAsync();
            foreach (var y in all.Where(y => y.IsCurrent))
            {
                y.IsCurrent = false;
                _unitOfWork.AcademicYears.Update(y);
            }
            year.IsCurrent = true;
            _unitOfWork.AcademicYears.Update(year);
            await _unitOfWork.SaveChangesAsync();
        });

        return OperationResult.Success("تم تعيين السنة الدراسية الحالية بنجاح");
    }

    public async Task<OperationResult<AcademicYearDto>> GetCurrentAcademicYearAsync()
    {
        var year = await _unitOfWork.AcademicYears.GetCurrentAsync();
        if (year is null)
            return OperationResult<AcademicYearDto>.Failure("لا توجد سنة دراسية حالية محددة");

        return OperationResult<AcademicYearDto>.Success(
            _mapper.Map<AcademicYearDto>(year),
            "تم جلب السنة الدراسية الحالية بنجاح");
    }

    public async Task<OperationResult<IEnumerable<AcademicYearDto>>> GetAllAcademicYearsAsync()
    {
        var years = await _unitOfWork.AcademicYears.GetAllOrderedByStartDateAsync();
        return OperationResult<IEnumerable<AcademicYearDto>>.Success(
            _mapper.Map<IEnumerable<AcademicYearDto>>(years),
            "تم جلب السنوات الدراسية بنجاح");
    }

    public async Task<OperationResult<AcademicYearDto>> GetAcademicYearByDateAsync(DateTime date)
    {
        var all = await _unitOfWork.AcademicYears.GetAllOrderedByStartDateAsync();
        var dateOnly = DateOnly.FromDateTime(date);
        var year = all.FirstOrDefault(y => !y.IsDeleted && y.StartDate <= dateOnly && y.EndDate >= dateOnly);
        if (year is null)
            return OperationResult<AcademicYearDto>.Failure("لا توجد سنة دراسية تطابق هذا التاريخ");

        return OperationResult<AcademicYearDto>.Success(
            _mapper.Map<AcademicYearDto>(year),
            "تم جلب السنة الدراسية بنجاح");
    }

    public async Task<OperationResult> ArchiveAcademicYearAsync(int id)
    {
        var entity = await _unitOfWork.AcademicYears.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("السنة الدراسية غير موجودة");

        if (entity.IsCurrent)
            return OperationResult.Failure("لا يمكن أرشفة السنة الدراسية الحالية");

        _unitOfWork.AcademicYears.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم أرشفة السنة الدراسية بنجاح");
    }
}
