using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.EvaluationItems;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class EvaluationItemService : IEvaluationItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public EvaluationItemService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<EvaluationItemDto>> CreateEvaluationItemAsync(
        CreateEvaluationItemRequest request)
    {
        var template = await _unitOfWork.EvaluationTemplates.GetByIdAsync(request.TemplateId);
        if (template is null || template.IsDeleted)
            return OperationResult<EvaluationItemDto>.Failure("قالب التقييم غير موجود");

        var items = await _unitOfWork.EvaluationItems.GetByTemplateIdAsync(request.TemplateId);
        if (items.Any(i => i.Name == request.Name && !i.IsDeleted))
            return OperationResult<EvaluationItemDto>.Failure("اسم المعيار مكرر في هذا القالب");

        if (request.MaxScore <= 0)
            return OperationResult<EvaluationItemDto>.Failure("الدرجة القصوى يجب أن تكون أكبر من صفر");

        if (request.Weight <= 0)
            return OperationResult<EvaluationItemDto>.Failure("الوزن يجب أن يكون أكبر من صفر");

        if (request.DisplayOrder <= 0)
            return OperationResult<EvaluationItemDto>.Failure("ترتيب العرض يجب أن يكون رقماً موجباً");

        var entity = _mapper.Map<EvaluationItem>(request);
        entity.IsVisible = true;

        await _unitOfWork.EvaluationItems.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<EvaluationItemDto>.Success(
            _mapper.Map<EvaluationItemDto>(entity),
            "تم إنشاء معيار التقييم بنجاح");
    }

    public async Task<OperationResult<EvaluationItemDto>> UpdateEvaluationItemAsync(
        UpdateEvaluationItemRequest request)
    {
        var entity = await _unitOfWork.EvaluationItems.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<EvaluationItemDto>.Failure("معيار التقييم غير موجود");

        var items = await _unitOfWork.EvaluationItems.GetByTemplateIdAsync(entity.TemplateId);
        if (items.Any(i => i.Id != request.Id && i.Name == request.Name && !i.IsDeleted))
            return OperationResult<EvaluationItemDto>.Failure("اسم المعيار مكرر في هذا القالب");

        entity.Name = request.Name;
        entity.MaxScore = request.MaxScore;
        entity.Weight = request.Weight;
        entity.AutoCalcType = request.AutoCalcType;
        entity.AbsenceMaxScore = request.AbsenceMaxScore;
        entity.DisplayOrder = request.DisplayOrder;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvaluationItems.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<EvaluationItemDto>.Success(
            _mapper.Map<EvaluationItemDto>(entity),
            "تم تحديث معيار التقييم بنجاح");
    }

    public async Task<OperationResult> ToggleItemVisibilityAsync(int id)
    {
        var entity = await _unitOfWork.EvaluationItems.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("معيار التقييم غير موجود");

        entity.IsVisible = !entity.IsVisible;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvaluationItems.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success(
            entity.IsVisible ? "تم إظهار معيار التقييم بنجاح" : "تم إخفاء معيار التقييم بنجاح");
    }

    public async Task<OperationResult> DeleteEvaluationItemAsync(int id)
    {
        var entity = await _unitOfWork.EvaluationItems.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("معيار التقييم غير موجود");

        _unitOfWork.EvaluationItems.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف معيار التقييم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<EvaluationItemDto>>> GetItemsByTemplateAsync(int templateId)
    {
        var template = await _unitOfWork.EvaluationTemplates.GetByIdAsync(templateId);
        if (template is null || template.IsDeleted)
            return OperationResult<IEnumerable<EvaluationItemDto>>.Failure("قالب التقييم غير موجود");

        var items = await _unitOfWork.EvaluationItems.GetOrderedByTemplateIdAsync(templateId);
        return OperationResult<IEnumerable<EvaluationItemDto>>.Success(
            _mapper.Map<IEnumerable<EvaluationItemDto>>(items),
            "تم جلب معايير التقييم بنجاح");
    }

    public async Task<OperationResult<EvaluationItemDto>> GetItemByIdAsync(int id)
    {
        var entity = await _unitOfWork.EvaluationItems.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<EvaluationItemDto>.Failure("معيار التقييم غير موجود");

        return OperationResult<EvaluationItemDto>.Success(
            _mapper.Map<EvaluationItemDto>(entity),
            "تم جلب معيار التقييم بنجاح");
    }

    public async Task<OperationResult> ReorderItemsAsync(int templateId, List<int> orderedIds)
    {
        var template = await _unitOfWork.EvaluationTemplates.GetByIdAsync(templateId);
        if (template is null || template.IsDeleted)
            return OperationResult.Failure("قالب التقييم غير موجود");

        var items = await _unitOfWork.EvaluationItems.GetByTemplateIdAsync(templateId);
        var itemDict = items.ToDictionary(i => i.Id);

        for (int i = 0; i < orderedIds.Count; i++)
        {
            if (itemDict.TryGetValue(orderedIds[i], out var item))
            {
                item.DisplayOrder = i + 1;
                item.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.EvaluationItems.Update(item);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم إعادة ترتيب المعايير بنجاح");
    }
}
