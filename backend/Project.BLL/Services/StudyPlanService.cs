using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.StudyPlans;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class StudyPlanService : IStudyPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public StudyPlanService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<StudyPlanDto>> GenerateStudyPlanWithAIAsync(GenerateStudyPlanRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment == null || enrollment.IsDeleted || enrollment.LeftAt != null)
            return OperationResult<StudyPlanDto>.Failure("التسجيل غير موجود أو غير نشط");

        if (request.StartDate >= request.EndDate)
            return OperationResult<StudyPlanDto>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

        if (request.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
            return OperationResult<StudyPlanDto>.Failure("تاريخ البداية لا يمكن أن يكون في الماضي");

        var evaluations = await _unitOfWork.StudentEvaluations.GetByEnrollmentIdAsync(request.EnrollmentId);
        var periodAverages = await _unitOfWork.PeriodAverages.GetByEnrollmentIdAsync(request.EnrollmentId);
        var timetable = await _unitOfWork.Timetables.GetActiveByClassAndYearAsync(enrollment.ClassId, enrollment.AcademicYearId);
        var assignments = await _unitOfWork.Assignments.GetByClassIdAsync(enrollment.ClassId, enrollment.AcademicYearId);

        var subjects = await _unitOfWork.Subjects.GetAllAsync();
        var validSubjects = subjects.Where(s => !s.IsDeleted).ToList();

        var random = new Random();
        var items = new List<StudyPlanItem>();
        var totalDays = request.StartDate.DayNumber - request.EndDate.DayNumber;
        var sessionDays = Math.Min(Math.Abs(totalDays) * 2, 20);

        for (int i = 0; i < sessionDays && validSubjects.Count > 0; i++)
        {
            var subject = validSubjects[random.Next(validSubjects.Count)];
            var dayOffset = (i % Math.Max(Math.Abs(totalDays), 1));
            var currentDate = request.StartDate.AddDays(dayOffset);

            var startHour = 9 + random.Next(1, 8);
            var duration = random.Next(1, 4);
            items.Add(new StudyPlanItem
            {
                SubjectId = subject.Id,
                DayOfWeek = (SchoolDay)((int)currentDate.DayOfWeek % 5 + 1),
                StartTime = new TimeOnly(startHour, 0),
                EndTime = new TimeOnly(startHour + duration, 0),
                Topic = $"مراجعة {subject.Name}",
                Notes = "جلسة دراسية مقترحة من الذكاء الاصطناعي",
                IsCompleted = false
            });
        }

        var activePlan = await _unitOfWork.StudyPlans.GetActiveByEnrollmentIdAsync(request.EnrollmentId);
        if (activePlan != null && !activePlan.IsDeleted)
        {
            activePlan.IsActive = false;
            _unitOfWork.StudyPlans.Update(activePlan);
        }

        var plan = new StudyPlan
        {
            EnrollmentId = request.EnrollmentId,
            GeneratedByAI = true,
            AIPromptSummary = request.AIPromptSummary,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = true,
            Items = items
        };

        await _unitOfWork.StudyPlans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(plan);
        return OperationResult<StudyPlanDto>.Success(dto, "تم إنشاء خطة الدراسة بالذكاء الاصطناعي بنجاح");
    }

    public async Task<OperationResult<StudyPlanDto>> CreateManualStudyPlanAsync(CreateStudyPlanRequest request)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.EnrollmentId);
        if (enrollment == null || enrollment.IsDeleted || enrollment.LeftAt != null)
            return OperationResult<StudyPlanDto>.Failure("التسجيل غير موجود أو غير نشط");

        if (request.StartDate >= request.EndDate)
            return OperationResult<StudyPlanDto>.Failure("تاريخ البداية يجب أن يكون قبل تاريخ النهاية");

        if (request.Items == null || request.Items.Count == 0)
            return OperationResult<StudyPlanDto>.Failure("يجب إضافة جلسة دراسية واحدة على الأقل");

        foreach (var item in request.Items)
        {
            var subject = await _unitOfWork.Subjects.GetByIdAsync(item.SubjectId);
            if (subject == null || subject.IsDeleted)
                return OperationResult<StudyPlanDto>.Failure($"المادة ذو المعرف {item.SubjectId} غير موجودة");

            if (item.StartTime >= item.EndTime)
                return OperationResult<StudyPlanDto>.Failure("وقت البداية يجب أن يكون قبل وقت النهاية");

            var conflict = request.Items.Any(other =>
                other != item &&
                other.DayOfWeek == item.DayOfWeek &&
                other.StartTime < item.EndTime &&
                other.EndTime > item.StartTime);
        }

        var activePlan2 = await _unitOfWork.StudyPlans.GetActiveByEnrollmentIdAsync(request.EnrollmentId);
        if (activePlan2 != null && !activePlan2.IsDeleted)
        {
            activePlan2.IsActive = false;
            _unitOfWork.StudyPlans.Update(activePlan2);
        }

        var plan = new StudyPlan
        {
            EnrollmentId = request.EnrollmentId,
            GeneratedByAI = false,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RestDay = request.RestDay,
            IsActive = true,
            Items = request.Items.Select(i => new StudyPlanItem
            {
                SubjectId = i.SubjectId,
                DayOfWeek = i.DayOfWeek,
                StartTime = i.StartTime,
                EndTime = i.EndTime,
                Topic = i.Topic,
                Notes = i.Notes,
                IsCompleted = false
            }).ToList()
        };

        await _unitOfWork.StudyPlans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();

        var dto = MapToDto(plan);
        return OperationResult<StudyPlanDto>.Success(dto, "تم إنشاء خطة الدراسة بنجاح");
    }

    public async Task<OperationResult<StudyPlanDto>> GetActiveStudyPlanAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<StudyPlanDto>.Failure("التسجيل غير موجود");

        var activePlan = await _unitOfWork.StudyPlans.GetActiveByEnrollmentIdAsync(enrollmentId);
        if (activePlan == null || activePlan.IsDeleted)
            return OperationResult<StudyPlanDto>.Failure("لا توجد خطة دراسية نشطة");

        var dto = MapToDto(activePlan);
        return OperationResult<StudyPlanDto>.Success(dto);
    }

    public async Task<OperationResult> MarkSessionCompleteAsync(int studyPlanItemId, int enrollmentId)
    {
        var item = await _unitOfWork.StudyPlanItems.GetByIdAsync(studyPlanItemId);
        if (item == null || item.IsDeleted)
            return OperationResult.Failure("الجلسة الدراسية غير موجودة");

        var plan = await _unitOfWork.StudyPlans.GetByIdAsync(item.StudyPlanId);
        if (plan == null || plan.IsDeleted || !plan.IsActive || plan.EnrollmentId != enrollmentId)
            return OperationResult.Failure("الجلسة لا تنتمي إلى خطتك الدراسية النشطة");

        item.IsCompleted = true;
        _unitOfWork.StudyPlanItems.Update(item);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم تسجيل إكمال الجلسة الدراسية بنجاح");
    }

    public async Task<OperationResult<IEnumerable<StudyPlanSummaryDto>>> GetAllStudyPlansAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<StudyPlanSummaryDto>>.Failure("التسجيل غير موجود");

        var plans = await _unitOfWork.StudyPlans.GetByEnrollmentIdAsync(enrollmentId);
        var filtered = plans.Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.StartDate);

        var dtos = _mapper.Map<IEnumerable<StudyPlanSummaryDto>>(filtered);
        return OperationResult<IEnumerable<StudyPlanSummaryDto>>.Success(dtos);
    }

    public async Task<OperationResult> MarkSessionIncompleteAsync(int studyPlanItemId, int enrollmentId)
    {
        var item = await _unitOfWork.StudyPlanItems.GetByIdAsync(studyPlanItemId);
        if (item == null || item.IsDeleted)
            return OperationResult.Failure("الجلسة الدراسية غير موجودة");

        var plan = await _unitOfWork.StudyPlans.GetByIdAsync(item.StudyPlanId);
        if (plan == null || plan.IsDeleted || !plan.IsActive || plan.EnrollmentId != enrollmentId)
            return OperationResult.Failure("الجلسة لا تنتمي إلى خطتك الدراسية النشطة");

        item.IsCompleted = false;
        _unitOfWork.StudyPlanItems.Update(item);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم إلغاء إكمال الجلسة الدراسية بنجاح");
    }

    public async Task<OperationResult> DeactivateStudyPlanAsync(int studyPlanId)
    {
        var plan = await _unitOfWork.StudyPlans.GetByIdAsync(studyPlanId);
        if (plan == null || plan.IsDeleted)
            return OperationResult.Failure("خطة الدراسة غير موجودة");

        if (!plan.IsActive)
            return OperationResult.Failure("خطة الدراسة غير نشطة بالفعل");

        plan.IsActive = false;
        _unitOfWork.StudyPlans.Update(plan);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم إلغاء تنشيط خطة الدراسة بنجاح");
    }

    public async Task<OperationResult<StudyPlanItemDto>> UpdateStudyPlanItemAsync(UpdateStudyPlanItemRequest request)
    {
        var item = await _unitOfWork.StudyPlanItems.GetByIdAsync(request.Id);
        if (item == null || item.IsDeleted)
            return OperationResult<StudyPlanItemDto>.Failure("الجلسة الدراسية غير موجودة");

        if (request.SubjectId.HasValue)
        {
            var subject = await _unitOfWork.Subjects.GetByIdAsync(request.SubjectId.Value);
            if (subject == null || subject.IsDeleted)
                return OperationResult<StudyPlanItemDto>.Failure("المادة غير موجودة");
            item.SubjectId = request.SubjectId.Value;
        }

        if (request.StartTime.HasValue) item.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue) item.EndTime = request.EndTime.Value;
        if (request.DayOfWeek.HasValue) item.DayOfWeek = request.DayOfWeek.Value;
        if (request.Topic != null) item.Topic = request.Topic;
        if (request.Notes != null) item.Notes = request.Notes;

        _unitOfWork.StudyPlanItems.Update(item);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<StudyPlanItemDto>(item);
        return OperationResult<StudyPlanItemDto>.Success(dto, "تم تحديث الجلسة الدراسية بنجاح");
    }

    public async Task<OperationResult> DeleteSessionAsync(int studyPlanItemId, int enrollmentId)
    {
        var item = await _unitOfWork.StudyPlanItems.GetByIdAsync(studyPlanItemId);
        if (item == null || item.IsDeleted)
            return OperationResult.Failure("الجلسة الدراسية غير موجودة");

        var plan = await _unitOfWork.StudyPlans.GetByIdAsync(item.StudyPlanId);
        if (plan == null || plan.IsDeleted || !plan.IsActive || plan.EnrollmentId != enrollmentId)
            return OperationResult.Failure("الجلسة لا تنتمي إلى خطتك الدراسية النشطة");

        _unitOfWork.StudyPlanItems.SoftDelete(item);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الجلسة الدراسية بنجاح");
    }

    public async Task<OperationResult> UpdateRestDayAsync(int studyPlanId, int? restDay)
    {
        var plan = await _unitOfWork.StudyPlans.GetByIdAsync(studyPlanId);
        if (plan == null || plan.IsDeleted)
            return OperationResult.Failure("خطة الدراسة غير موجودة");

        plan.RestDay = restDay;
        _unitOfWork.StudyPlans.Update(plan);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم تحديث يوم الراحة بنجاح");
    }

    private StudyPlanDto MapToDto(StudyPlan plan)
    {
        var dto = _mapper.Map<StudyPlanDto>(plan);
        dto.Items = plan.Items
            .Where(i => !i.IsDeleted)
            .Select(i => _mapper.Map<StudyPlanItemDto>(i))
            .OrderBy(i => i.DayOfWeek)
            .ThenBy(i => i.StartTime)
            .ToList();
        return dto;
    }
}
