using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.StudentEvaluations;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

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

    public async Task<OperationResult> AutoFillAttendanceScoresAsync(int classId, int periodId, int enteredById)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult.Failure("الفصل غير موجود");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period is null || period.IsDeleted)
            return OperationResult.Failure("فترة التقييم غير موجودة");

        if (period.StartDate is null || period.EndDate is null)
            return OperationResult.Failure("الفترة يجب أن تحتوي على تاريخ بداية ونهاية");

        var enrollments = await _unitOfWork.StudentEnrollments
            .GetActiveByClassAsync(classId, classEntity.AcademicYearId);

        if (enrollments.Count == 0)
            return OperationResult.Success("لا يوجد طلاب في هذا الفصل");

        var cstList = await _unitOfWork.ClassSubjectTeachers
            .FindAsync(cst => cst.ClassId == classId && !cst.IsDeleted);

        if (cstList.Count == 0)
            return OperationResult.Success("لا يوجد مواد مرتبطة بهذا الفصل");

        var subjectIds = cstList.Select(cst => cst.SubjectId).Distinct().ToHashSet();

        var templates = await _unitOfWork.EvaluationTemplates
            .FindAsync(t => t.GradeLevelId == classEntity.GradeLevelId
                         && t.AcademicYearId == classEntity.AcademicYearId
                         && !t.IsDeleted);

        var matchingTemplates = templates.Where(t => subjectIds.Contains(t.SubjectId)).ToList();
        if (matchingTemplates.Count == 0)
            return OperationResult.Success("لا يوجد قوالب تقييم مرتبطة بهذا الفصل");

        var templateIds = matchingTemplates.Select(t => t.Id).ToList();
        var allItems = await _unitOfWork.EvaluationItems
            .FindAsync(i => templateIds.Contains(i.TemplateId)
                         && i.AutoCalcType == AutoCalcType.Attendance
                         && !i.IsDeleted
                         && i.IsVisible);

        if (allItems.Count == 0)
            return OperationResult.Success("لا يوجد بنود تقييم تعتمد على الغياب في قوالب هذا الفصل");

        var totalSchoolDays = CountSchoolDays(period.StartDate.Value, period.EndDate.Value);
        if (totalSchoolDays == 0)
            return OperationResult.Failure("لا توجد أيام دراسية في هذه الفترة");

        var autoFilled = 0;
        foreach (var enrollment in enrollments)
        {
            var absentCount = await _unitOfWork.DailyAbsences.GetAbsenceCountAsync(
                enrollment.Id, null, period.StartDate, period.EndDate);

            var presentCount = totalSchoolDays - absentCount;
            var attendanceRate = (decimal)presentCount / totalSchoolDays;

            foreach (var item in allItems)
            {
                var absencePortion = item.AbsenceMaxScore ?? item.MaxScore;
                var lost = Math.Round(absencePortion * (1 - attendanceRate), 2);
                var score = Math.Round(item.MaxScore - lost, 2);

                var existing = await _unitOfWork.StudentEvaluations
                    .GetByEnrollmentItemAndPeriodAsync(enrollment.Id, item.Id, periodId);

                if (existing is not null && !existing.IsDeleted)
                {
                    existing.Score = score;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.StudentEvaluations.Update(existing);
                }
                else
                {
                    var eval = new StudentEvaluation
                    {
                        EnrollmentId = enrollment.Id,
                        EvaluationItemId = item.Id,
                        PeriodId = periodId,
                        Score = score,
                        EnteredById = enteredById,
                        EnteredAt = DateTime.UtcNow
                    };
                    await _unitOfWork.StudentEvaluations.AddAsync(eval);
                }
                autoFilled++;
            }
        }

        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success($"تم تعبئة {autoFilled} درجة تلقائياً بناءً على الغياب");
    }

    private static int CountSchoolDays(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek >= 0 && dayOfWeek <= 4)
                count++;
        }
        return count;
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
        entity.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.StudentEvaluations.Update(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult<StudentEvaluationDto>.Success(
            _mapper.Map<StudentEvaluationDto>(entity),
            "تم تحديث التقييم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<StudentEvaluationDto>>> GetByEnrollmentAndPeriodAsync(
        int enrollmentId, int periodId, AcademicTerm? term = null)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<IEnumerable<StudentEvaluationDto>>.Failure("القيد غير موجود");

        var period = await _unitOfWork.EvaluationPeriods.GetByIdAsync(periodId);
        if (period is null || period.IsDeleted)
            return OperationResult<IEnumerable<StudentEvaluationDto>>.Failure("فترة التقييم غير موجودة");

        var evaluations = await _unitOfWork.StudentEvaluations.GetByEnrollmentAndPeriodAsync(enrollmentId, periodId);

        // Filter by term if specified
        if (term.HasValue)
            evaluations = evaluations.Where(e => e.Period?.SemesterNumber == (int)term.Value).ToList();

        return OperationResult<IEnumerable<StudentEvaluationDto>>.Success(
            _mapper.Map<IEnumerable<StudentEvaluationDto>>(evaluations),
            "تم جلب التقييمات بنجاح");
    }

    public async Task<OperationResult<StudentEvaluationDto>> GetEvaluationByIdAsync(int id)
    {
        var entity = await _unitOfWork.StudentEvaluations.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult<StudentEvaluationDto>.Failure("التقييم غير موجود");
        return OperationResult<StudentEvaluationDto>.Success(
            _mapper.Map<StudentEvaluationDto>(entity), "");
    }

    public async Task<OperationResult> DeleteEvaluationAsync(int id)
    {
        var entity = await _unitOfWork.StudentEvaluations.GetByIdAsync(id);
        if (entity is null || entity.IsDeleted)
            return OperationResult.Failure("التقييم غير موجود");

        _unitOfWork.StudentEvaluations.SoftDelete(entity);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف التقييم بنجاح");
    }

    public async Task<OperationResult> BulkSaveEvaluationsAsync(BulkSaveEvaluationsRequest request)
    {
        if (request.Entries.Count == 0)
            return OperationResult.Success("لا توجد تغييرات للحفظ");

        var items = await _unitOfWork.EvaluationItems
            .FindAsync(i => request.Entries.Select(e => e.EvaluationItemId).Distinct().Contains(i.Id));
        var itemMap = items.ToDictionary(i => i.Id);

        var saved = 0;
        foreach (var entry in request.Entries)
        {
            if (!itemMap.TryGetValue(entry.EvaluationItemId, out var item))
                continue;

            if (!entry.Score.HasValue) continue;
            if (entry.Score < 0 || entry.Score > item.MaxScore) continue;

            // Round up to nearest 0.5 (e.g. 7.1→7.5, 7.6→8)
            entry.Score = Math.Ceiling(entry.Score.Value * 2) / 2;

            if (entry.EvaluationId.HasValue)
            {
                var existing = await _unitOfWork.StudentEvaluations.GetByIdAsync(entry.EvaluationId.Value);
                if (existing is null || existing.IsDeleted) continue;
                existing.Score = entry.Score;
                existing.EnteredById = request.EnteredById;
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.StudentEvaluations.Update(existing);
            }
            else
            {
                var existing = await _unitOfWork.StudentEvaluations
                    .GetByEnrollmentItemAndPeriodAsync(entry.EnrollmentId, entry.EvaluationItemId, entry.PeriodId);
                if (existing is not null && !existing.IsDeleted)
                {
                    existing.Score = entry.Score;
                    existing.EnteredById = request.EnteredById;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.StudentEvaluations.Update(existing);
                }
                else
                {
                    var eval = new StudentEvaluation
                    {
                        EnrollmentId = entry.EnrollmentId,
                        EvaluationItemId = entry.EvaluationItemId,
                        PeriodId = entry.PeriodId,
                        Score = entry.Score,
                        EnteredById = request.EnteredById,
                        EnteredAt = DateTime.UtcNow
                    };
                    await _unitOfWork.StudentEvaluations.AddAsync(eval);
                }
            }
            saved++;
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult.Success("تم حفظ الدرجات بنجاح");
    }

    public async Task<OperationResult<IEnumerable<ClassEvaluationDto>>> GetByClassAndPeriodAsync(
        int classId, int periodId, AcademicTerm? term = null)
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

        // Filter by term if specified
        if (term.HasValue)
            evaluations = evaluations.Where(e => e.Period?.SemesterNumber == (int)term.Value).ToList();

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
