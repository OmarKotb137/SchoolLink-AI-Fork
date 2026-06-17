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
                request.GradeLevelId, request.SubjectId, request.AcademicYearId, request.Term))
            return OperationResult<EvaluationTemplateDto>.Failure("يوجد قالب تقييم لهذا الصف والمادة والسنة بالفعل");

        int weeks;
        if (request.Term.HasValue)
        {
            // ── Semester-specific template ──
            DateOnly semStart, semEnd;
            if (request.Term.Value == AcademicTerm.FirstSemester)
            {
                semStart = year.FirstSemesterStartDate ?? year.StartDate;
                semEnd   = year.FirstSemesterEndDate   ?? year.EndDate;
            }
            else // SecondSemester
            {
                semStart = year.SecondSemesterStartDate ?? year.StartDate;
                semEnd   = year.SecondSemesterEndDate   ?? year.EndDate;
            }

            // Auto-calculate weeks from semester duration
            var totalDays = semEnd.DayNumber - semStart.DayNumber;
            weeks = Math.Max(1, (totalDays + 6) / 7);

            // Generate periods only for this semester if not already created
            var semesterPeriods = await _unitOfWork.EvaluationPeriods.GetByTypeAndYearAndSemesterAsync(
                request.AcademicYearId, PeriodType.Weekly, (int)request.Term.Value);
            if (semesterPeriods.Count == 0)
            {
                var periods = Project.Domain.Helpers.EvaluationPeriodGenerator.GeneratePeriodsForSemester(
                    request.AcademicYearId, semStart, semEnd, (int)request.Term.Value);
                foreach (var p in periods)
                    await _unitOfWork.EvaluationPeriods.AddAsync(p);
            }
        }
        else
        {
            // ── Whole-year template (fallback) ──
            var existingPeriods = await _unitOfWork.EvaluationPeriods.GetByAcademicYearAsync(request.AcademicYearId);
            if (existingPeriods.Count == 0)
            {
                IReadOnlyList<EvaluationPeriod> periods;
                if (year.FirstSemesterStartDate.HasValue && year.FirstSemesterEndDate.HasValue &&
                    year.SecondSemesterStartDate.HasValue && year.SecondSemesterEndDate.HasValue)
                {
                    periods = Project.Domain.Helpers.EvaluationPeriodGenerator.GeneratePeriods(
                        request.AcademicYearId, year.StartDate,
                        year.FirstSemesterStartDate, year.FirstSemesterEndDate,
                        year.SecondSemesterStartDate, year.SecondSemesterEndDate);
                }
                else
                {
                    periods = Project.Domain.Helpers.EvaluationPeriodGenerator.GeneratePeriods(
                        request.AcademicYearId, year.StartDate, year.EndDate);
                }

                foreach (var p in periods)
                    await _unitOfWork.EvaluationPeriods.AddAsync(p);
            }

            weeks = request.Weeks > 0 ? request.Weeks : 12;
        }

        var entity = _mapper.Map<EvaluationTemplate>(request);
        entity.IsActive = true;
        entity.Term = request.Term;
        entity.Weeks = weeks;

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
        entity.Weeks = request.Weeks;
        entity.Term = request.Term;
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
                source.GradeLevelId, source.SubjectId, source.AcademicYearId, source.Term))
            return OperationResult<EvaluationTemplateDto>.Failure("يوجد قالب تقييم لهذا الصف والمادة والسنة بالفعل");

        var duplicate = new EvaluationTemplate
        {
            GradeLevelId = source.GradeLevelId,
            SubjectId = source.SubjectId,
            AcademicYearId = source.AcademicYearId,
            Name = source.Name + " (نسخة)",
            CalculationType = source.CalculationType,
            Term = source.Term,
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
