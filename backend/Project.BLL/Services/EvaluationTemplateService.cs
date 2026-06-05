using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.EvaluationTemplates;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class EvaluationTemplateService : IEvaluationTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public EvaluationTemplateService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<EvaluationTemplateDto>> CreateEvaluationTemplateAsync(
        CreateEvaluationTemplateRequest request)
    {
        var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(request.GradeLevelId);
        if (gradeLevel is null || gradeLevel.IsDeleted)
            return OperationResult<EvaluationTemplateDto>.Failure("الصف الدراسي غير موجود");

        var subject = await _unitOfWork.Subjects.GetByIdAsync(request.SubjectId);
        if (subject is null || subject.IsDeleted)
            return OperationResult<EvaluationTemplateDto>.Failure("المادة غير موجودة");

        var year = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (year is null || year.IsDeleted)
            return OperationResult<EvaluationTemplateDto>.Failure("السنة الدراسية غير موجودة");

        if (await _unitOfWork.EvaluationTemplates.ExistsByGradeLevelSubjectAndYearAsync(
                request.GradeLevelId, request.SubjectId, request.AcademicYearId))
            return OperationResult<EvaluationTemplateDto>.Failure("يوجد قالب تقييم لهذا الصف والمادة والسنة بالفعل");

        var entity = _mapper.Map<EvaluationTemplate>(request);
        entity.IsActive = true;

        await _unitOfWork.EvaluationTemplates.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<EvaluationTemplateDto>.Success(
            _mapper.Map<EvaluationTemplateDto>(entity),
            "تم إنشاء قالب التقييم بنجاح");
    }

    public async Task<OperationResult<EvaluationTemplateDto>> UpdateEvaluationTemplateAsync(
        UpdateEvaluationTemplateRequest request)
    {
        var entity = await _unitOfWork.EvaluationTemplates.GetByIdAsync(request.Id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<EvaluationTemplateDto>.Failure("قالب التقييم غير موجود");

        entity.Name = request.Name;
        entity.CalculationType = request.CalculationType;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvaluationTemplates.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<EvaluationTemplateDto>.Success(
            _mapper.Map<EvaluationTemplateDto>(entity),
            "تم تحديث قالب التقييم بنجاح");
    }

    public async Task<OperationResult<EvaluationTemplateDto>> GetTemplateByIdAsync(int id)
    {
        var entity = await _unitOfWork.EvaluationTemplates.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<EvaluationTemplateDto>.Failure("قالب التقييم غير موجود");

        return OperationResult<EvaluationTemplateDto>.Success(
            _mapper.Map<EvaluationTemplateDto>(entity),
            "تم جلب قالب التقييم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<EvaluationTemplateDto>>> GetTemplateByGradeLevelAsync(
        int gradeLevelId, int academicYearId)
    {
        var templates = await _unitOfWork.EvaluationTemplates.GetByGradeLevelAndYearAsync(gradeLevelId, academicYearId);
        return OperationResult<IEnumerable<EvaluationTemplateDto>>.Success(
            _mapper.Map<IEnumerable<EvaluationTemplateDto>>(templates),
            "تم جلب قوالب التقييم بنجاح");
    }

    public async Task<OperationResult> DeleteEvaluationTemplateAsync(int id)
    {
        var entity = await _unitOfWork.EvaluationTemplates.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("قالب التقييم غير موجود");

        _unitOfWork.EvaluationTemplates.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف قالب التقييم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<EvaluationTemplateDto>>> GetAllTemplatesAsync()
    {
        var templates = await _unitOfWork.EvaluationTemplates.FindAsync(t => !t.IsDeleted);
        return OperationResult<IEnumerable<EvaluationTemplateDto>>.Success(
            _mapper.Map<IEnumerable<EvaluationTemplateDto>>(templates),
            "تم جلب كل قوالب التقييم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<EvaluationTemplateDto>>> GetTemplatesBySubjectAsync(int subjectId, int academicYearId)
    {
        var templates = await _unitOfWork.EvaluationTemplates.FindAsync(
            t => t.SubjectId == subjectId && t.AcademicYearId == academicYearId && !t.IsDeleted);
        return OperationResult<IEnumerable<EvaluationTemplateDto>>.Success(
            _mapper.Map<IEnumerable<EvaluationTemplateDto>>(templates),
            "تم جلب قوالب التقييم للمادة بنجاح");
    }

    public async Task<OperationResult> ToggleTemplateActiveAsync(int id)
    {
        var entity = await _unitOfWork.EvaluationTemplates.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("قالب التقييم غير موجود");

        entity.IsActive = !entity.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.EvaluationTemplates.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success(
            entity.IsActive ? "تم تفعيل قالب التقييم بنجاح" : "تم إلغاء تفعيل قالب التقييم بنجاح");
    }

    public async Task<OperationResult<EvaluationTemplateDto>> DuplicateTemplateAsync(int id)
    {
        var source = await _unitOfWork.EvaluationTemplates.GetWithItemsAsync(id);
        if (source is null || source.IsDeleted)
            return OperationResult<EvaluationTemplateDto>.Failure("قالب التقييم غير موجود");

        if (await _unitOfWork.EvaluationTemplates.ExistsByGradeLevelSubjectAndYearAsync(
                source.GradeLevelId, source.SubjectId, source.AcademicYearId))
            return OperationResult<EvaluationTemplateDto>.Failure("يوجد قالب تقييم لهذا الصف والمادة والسنة بالفعل");

        var duplicate = new EvaluationTemplate
        {
            GradeLevelId = source.GradeLevelId,
            SubjectId = source.SubjectId,
            AcademicYearId = source.AcademicYearId,
            Name = source.Name + " (نسخة)",
            CalculationType = source.CalculationType,
            IsActive = false
        };

        await _unitOfWork.EvaluationTemplates.AddAsync(duplicate);
        await _unitOfWork.SaveChangesAsync();

        if (source.Items?.Any() == true)
        {
            foreach (var item in source.Items.Where(i => !i.IsDeleted))
            {
                var newItem = new EvaluationItem
                {
                    TemplateId = duplicate.Id,
                    Name = item.Name,
                    MaxScore = item.MaxScore,
                    Weight = item.Weight,
                    ItemType = item.ItemType,
                    DisplayOrder = item.DisplayOrder,
                    IsVisible = item.IsVisible,
                    AutoCalcType = item.AutoCalcType
                };
                await _unitOfWork.EvaluationItems.AddAsync(newItem);
            }
            await _unitOfWork.SaveChangesAsync();
        }

        return OperationResult<EvaluationTemplateDto>.Success(
            _mapper.Map<EvaluationTemplateDto>(duplicate),
            "تم نسخ قالب التقييم بنجاح");
    }
}
