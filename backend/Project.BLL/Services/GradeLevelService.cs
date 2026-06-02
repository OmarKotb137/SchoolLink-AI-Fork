using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class GradeLevelService : IGradeLevelService
{
    private static readonly HashSet<string> ValidStages =
        new() { "ابتدائي", "إعدادي", "ثانوي" };

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public GradeLevelService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<GradeLevelDto>> CreateGradeLevelAsync(
        CreateGradeLevelRequest request)
    {
        // 1. Name uniqueness
        var existing = await _unitOfWork.GradeLevels.GetByNameAsync(request.Name);
        if (existing is not null && !existing.IsDeleted)
            return OperationResult<GradeLevelDto>.Failure("اسم الصف الدراسي موجود بالفعل");

        // 2. Stage validation
        if (request.Stage is not null && !ValidStages.Contains(request.Stage))
            return OperationResult<GradeLevelDto>.Failure("قيمة المرحلة التعليمية غير صحيحة");

        // 3. Create
        var entity = _mapper.Map<GradeLevel>(request);
        await _unitOfWork.GradeLevels.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<GradeLevelDto>.Success(
            _mapper.Map<GradeLevelDto>(entity),
            "تم إنشاء الصف الدراسي بنجاح");
    }

    public async Task<OperationResult<GradeLevelDto>> UpdateGradeLevelAsync(
        UpdateGradeLevelRequest request)
    {
        // 1. Find entity
        var entity = await _unitOfWork.GradeLevels.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<GradeLevelDto>.Failure("الصف الدراسي غير موجود");

        // 2. Name uniqueness (excluding self)
        var existing = await _unitOfWork.GradeLevels.GetByNameAsync(request.Name);
        if (existing is not null && !existing.IsDeleted && existing.Id != request.Id)
            return OperationResult<GradeLevelDto>.Failure("اسم الصف الدراسي مستخدم بالفعل");

        // 3. Stage validation
        if (request.Stage is not null && !ValidStages.Contains(request.Stage))
            return OperationResult<GradeLevelDto>.Failure("قيمة المرحلة التعليمية غير صحيحة");

        // 4. Apply updates
        entity.Name       = request.Name;
        entity.Stage      = request.Stage;
        entity.LevelOrder = request.LevelOrder;
        entity.UpdatedAt  = DateTime.UtcNow;

        _unitOfWork.GradeLevels.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<GradeLevelDto>.Success(
            _mapper.Map<GradeLevelDto>(entity),
            "تم تحديث الصف الدراسي بنجاح");
    }

    public async Task<OperationResult> DeleteGradeLevelAsync(int id)
    {
        // Grade levels cannot be removed while classes or templates depend on them.
        var entity = await _unitOfWork.GradeLevels.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("الصف الدراسي غير موجود");

        if (await _unitOfWork.Classes.AnyAsync(c => c.GradeLevelId == id) ||
            await _unitOfWork.EvaluationTemplates.AnyAsync(t => t.GradeLevelId == id))
            return OperationResult.Failure("لا يمكن حذف صف دراسي مستخدم في بيانات أخرى");

        _unitOfWork.GradeLevels.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الصف الدراسي بنجاح");
    }

    public async Task<OperationResult<GradeLevelDto>> GetGradeLevelByIdAsync(int id)
    {
        var entity = await _unitOfWork.GradeLevels.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<GradeLevelDto>.Failure("الصف الدراسي غير موجود");

        return OperationResult<GradeLevelDto>.Success(
            _mapper.Map<GradeLevelDto>(entity),
            "تم جلب الصف الدراسي بنجاح");
    }

    public async Task<OperationResult<IEnumerable<GradeLevelDto>>> GetAllGradeLevelsAsync()
    {
        var list = await _unitOfWork.GradeLevels.GetAllOrderedAsync();
        return OperationResult<IEnumerable<GradeLevelDto>>.Success(
            _mapper.Map<IEnumerable<GradeLevelDto>>(list),
            "تم جلب الصفوف الدراسية بنجاح");
    }
}
