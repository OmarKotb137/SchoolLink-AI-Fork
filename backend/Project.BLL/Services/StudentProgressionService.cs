using Common.Results;
using Project.BLL.DTOs.Enrollments;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class StudentProgressionService : IStudentProgressionService
{
    private readonly IUnitOfWork _unitOfWork;

    public StudentProgressionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OperationResult<IEnumerable<StudentProgressionCandidateDto>>> GetCandidatesAsync(
        int gradeLevelId,
        int academicYearId,
        CancellationToken ct = default)
    {
        var gradeLevel = await _unitOfWork.GradeLevels.GetByIdAsync(gradeLevelId, ct);
        if (gradeLevel is null || gradeLevel.IsDeleted)
            return OperationResult<IEnumerable<StudentProgressionCandidateDto>>.Failure("الصف الدراسي غير موجود");

        var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId, ct);
        if (academicYear is null || academicYear.IsDeleted)
            return OperationResult<IEnumerable<StudentProgressionCandidateDto>>.Failure("السنة الدراسية غير موجودة");

        var enrollments = await _unitOfWork.StudentEnrollments
            .GetActiveByGradeLevelAndYearWithDetailsAsync(gradeLevelId, academicYearId, ct);

        if (enrollments.Count == 0)
            return OperationResult<IEnumerable<StudentProgressionCandidateDto>>.Success(
                Array.Empty<StudentProgressionCandidateDto>(),
                "لا يوجد طلاب نشطون في الصف والسنة المحددين");

        var enrollmentIds = enrollments.Select(e => e.Id).ToList();
        var finalGrades = await _unitOfWork.FinalGrades.FindAsync(
            fg => !fg.IsDeleted && enrollmentIds.Contains(fg.EnrollmentId),
            ct);

        var finalGradesByEnrollmentId = finalGrades.ToDictionary(fg => fg.EnrollmentId);

        var candidates = enrollments
            .OrderBy(e => e.Student.FullName)
            .Select(enrollment =>
            {
                finalGradesByEnrollmentId.TryGetValue(enrollment.Id, out var finalGrade);

                return new StudentProgressionCandidateDto
                {
                    EnrollmentId = enrollment.Id,
                    StudentId = enrollment.StudentId,
                    StudentName = enrollment.Student.FullName,
                    CurrentClassId = enrollment.ClassId,
                    CurrentClassName = enrollment.Class.Name,
                    CurrentGradeLevelId = enrollment.Class.GradeLevelId,
                    CurrentGradeLevelName = enrollment.Class.GradeLevel.Name,
                    AcademicYearId = enrollment.AcademicYearId,
                    AcademicYearName = academicYear.Name,
                    StudentIsActive = enrollment.Student.IsActive,
                    HasStudentAccount = enrollment.Student.UserId.HasValue,
                    HasFinalGrade = finalGrade is not null,
                    HasPublishedFinalGrade = finalGrade?.IsPublished ?? false,
                    FinalTotal = finalGrade is { IsPublished: true } ? finalGrade.Total : null
                };
            })
            .ToList();

        return OperationResult<IEnumerable<StudentProgressionCandidateDto>>.Success(candidates);
    }

    public async Task<OperationResult<StudentProgressionResultDto>> ExecuteAsync(
        StudentProgressionRequest request,
        CancellationToken ct = default)
    {
        if (request is null)
            return OperationResult<StudentProgressionResultDto>.Failure("بيانات الطلب غير صالحة");

        var enrollmentIds = request.EnrollmentIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (enrollmentIds.Count == 0)
            return OperationResult<StudentProgressionResultDto>.Failure("يجب اختيار طالب واحد على الأقل");

        if (!Enum.IsDefined(request.Action))
            return OperationResult<StudentProgressionResultDto>.Failure("نوع العملية غير صالح");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.EffectiveDate > today)
            return OperationResult<StudentProgressionResultDto>.Failure("تاريخ التنفيذ لا يمكن أن يكون في المستقبل");

        var selectedEnrollments = await _unitOfWork.StudentEnrollments.GetByIdsWithDetailsAsync(enrollmentIds, ct);
        if (selectedEnrollments.Count != enrollmentIds.Count)
            return OperationResult<StudentProgressionResultDto>.Failure("بعض القيود الدراسية المحددة غير موجودة");

        if (selectedEnrollments.Any(e => e.IsDeleted))
            return OperationResult<StudentProgressionResultDto>.Failure("الطلب يحتوي على قيد دراسي محذوف");

        if (selectedEnrollments.Any(e => e.LeftAt is not null))
            return OperationResult<StudentProgressionResultDto>.Failure("الطلب يحتوي على قيد دراسي مغلق بالفعل");

        if (selectedEnrollments.Any(e => e.Class is null || e.Class.IsDeleted || e.Class.GradeLevel is null))
            return OperationResult<StudentProgressionResultDto>.Failure("بعض القيود الدراسية تفتقد بيانات الصف الحالي");

        if (selectedEnrollments.Any(e => e.AcademicYear is null || e.AcademicYear.IsDeleted))
            return OperationResult<StudentProgressionResultDto>.Failure("بعض القيود الدراسية تفتقد بيانات السنة الدراسية");

        if (selectedEnrollments.Any(e => request.EffectiveDate < e.EnrolledAt))
            return OperationResult<StudentProgressionResultDto>.Failure("تاريخ التنفيذ لا يمكن أن يسبق تاريخ القيد الحالي");

        var sourceGradeLevelIds = selectedEnrollments
            .Select(e => e.Class.GradeLevelId)
            .Distinct()
            .ToList();
        if (sourceGradeLevelIds.Count != 1)
            return OperationResult<StudentProgressionResultDto>.Failure("يجب أن تنتمي القيود المختارة إلى صف دراسي مصدر واحد فقط");

        var sourceAcademicYearIds = selectedEnrollments
            .Select(e => e.AcademicYearId)
            .Distinct()
            .ToList();
        if (sourceAcademicYearIds.Count != 1)
            return OperationResult<StudentProgressionResultDto>.Failure("يجب أن تنتمي القيود المختارة إلى سنة دراسية مصدر واحدة فقط");

        var sourceGradeLevel = selectedEnrollments[0].Class.GradeLevel;
        var sourceAcademicYear = selectedEnrollments[0].AcademicYear;
        var nextGradeLevel = await _unitOfWork.GradeLevels.GetByLevelOrderAsync(sourceGradeLevel.LevelOrder + 1, ct);
        if (nextGradeLevel?.IsDeleted == true)
            nextGradeLevel = null;

        AcademicYear? targetAcademicYear = null;
        SchoolClass? targetClass = null;
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

        switch (request.Action)
        {
            case StudentProgressionActionType.Promote:
                if (nextGradeLevel is null)
                    return OperationResult<StudentProgressionResultDto>.Failure(
                        "لا يوجد صف تالٍ لهذا الصف. استخدم التخرج إذا كان هذا آخر صف في المدرسة");

                if (!request.TargetAcademicYearId.HasValue)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف مطلوبة للترقية");

                if (!request.TargetClassId.HasValue)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف مطلوب للترقية");

                targetAcademicYear = await _unitOfWork.AcademicYears.GetByIdAsync(request.TargetAcademicYearId.Value, ct);
                if (targetAcademicYear is null || targetAcademicYear.IsDeleted)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف غير موجودة");

                if (targetAcademicYear.Id == sourceAcademicYear.Id)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف يجب أن تختلف عن السنة المصدر");

                if (targetAcademicYear.StartDate <= sourceAcademicYear.StartDate)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف يجب أن تبدأ بعد السنة المصدر");

                targetClass = await _unitOfWork.Classes.GetByIdWithIncludesAsync(request.TargetClassId.Value, ct);
                if (targetClass is null || targetClass.IsDeleted)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف غير موجود");

                if (targetClass.AcademicYearId != targetAcademicYear.Id)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف لا ينتمي إلى السنة الدراسية الهدف");

                if (targetClass.GradeLevelId != nextGradeLevel.Id)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف يجب أن ينتمي إلى الصف التالي مباشرة");
                break;

            case StudentProgressionActionType.Retain:
                if (!request.TargetAcademicYearId.HasValue)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف مطلوبة للإبقاء");

                if (!request.TargetClassId.HasValue)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف مطلوب للإبقاء");

                targetAcademicYear = await _unitOfWork.AcademicYears.GetByIdAsync(request.TargetAcademicYearId.Value, ct);
                if (targetAcademicYear is null || targetAcademicYear.IsDeleted)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف غير موجودة");

                if (targetAcademicYear.Id == sourceAcademicYear.Id)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف يجب أن تختلف عن السنة المصدر");

                if (targetAcademicYear.StartDate <= sourceAcademicYear.StartDate)
                    return OperationResult<StudentProgressionResultDto>.Failure("السنة الدراسية الهدف يجب أن تبدأ بعد السنة المصدر");

                targetClass = await _unitOfWork.Classes.GetByIdWithIncludesAsync(request.TargetClassId.Value, ct);
                if (targetClass is null || targetClass.IsDeleted)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف غير موجود");

                if (targetClass.AcademicYearId != targetAcademicYear.Id)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف لا ينتمي إلى السنة الدراسية الهدف");

                if (targetClass.GradeLevelId != sourceGradeLevel.Id)
                    return OperationResult<StudentProgressionResultDto>.Failure("الفصل الهدف يجب أن ينتمي إلى نفس الصف الدراسي المصدر");
                break;

            case StudentProgressionActionType.Graduate:
                if (nextGradeLevel is not null)
                    return OperationResult<StudentProgressionResultDto>.Failure(
                        "هذا الصف ليس الأخير في المدرسة، لذا لا يمكن تنفيذ التخرج عليه");

                if (request.TargetClassId.HasValue || request.TargetAcademicYearId.HasValue)
                    return OperationResult<StudentProgressionResultDto>.Failure(
                        "لا يجب إرسال سنة دراسية أو فصل هدف عند تنفيذ التخرج");
                break;
        }

        var result = new StudentProgressionResultDto
        {
            TotalRequested = enrollmentIds.Count
        };

        var deactivatedStudents = new HashSet<string>(StringComparer.Ordinal);
        var deactivatedParents = new HashSet<string>(StringComparer.Ordinal);
        var orderedSummaries = selectedEnrollments
            .OrderBy(e => e.Student.FullName)
            .ToList();

        foreach (var summary in orderedSummaries)
        {
            try
            {
                await _unitOfWork.BeginTransactionAsync(ct);

                var currentEnrollment = await _unitOfWork.StudentEnrollments.GetByIdWithDetailsAsync(summary.Id, ct);
                if (currentEnrollment is null || currentEnrollment.IsDeleted)
                {
                    await AddFailureAndRollbackAsync(
                        result,
                        summary.Id,
                        summary.StudentId,
                        summary.Student.FullName,
                        "القيد الدراسي لم يعد متاحًا",
                        ct);
                    continue;
                }

                if (currentEnrollment.LeftAt is not null)
                {
                    await AddFailureAndRollbackAsync(
                        result,
                        currentEnrollment.Id,
                        currentEnrollment.StudentId,
                        currentEnrollment.Student.FullName,
                        "القيد الدراسي مغلق بالفعل",
                        ct);
                    continue;
                }

                if (request.EffectiveDate < currentEnrollment.EnrolledAt)
                {
                    await AddFailureAndRollbackAsync(
                        result,
                        currentEnrollment.Id,
                        currentEnrollment.StudentId,
                        currentEnrollment.Student.FullName,
                        "تاريخ التنفيذ لا يمكن أن يسبق تاريخ القيد الحالي",
                        ct);
                    continue;
                }

                if (currentEnrollment.Class.GradeLevelId != sourceGradeLevel.Id ||
                    currentEnrollment.AcademicYearId != sourceAcademicYear.Id)
                {
                    await AddFailureAndRollbackAsync(
                        result,
                        currentEnrollment.Id,
                        currentEnrollment.StudentId,
                        currentEnrollment.Student.FullName,
                        "القيد الدراسي تغيّر أثناء التنفيذ ولم يعد مطابقًا للدفعة الحالية",
                        ct);
                    continue;
                }

                if (request.Action is StudentProgressionActionType.Promote or StudentProgressionActionType.Retain)
                {
                    if (targetAcademicYear is null || targetClass is null)
                    {
                        await AddFailureAndRollbackAsync(
                            result,
                            currentEnrollment.Id,
                            currentEnrollment.StudentId,
                            currentEnrollment.Student.FullName,
                            "بيانات الوجهة غير مكتملة",
                            ct);
                        continue;
                    }

                    var hasActiveEnrollment = await _unitOfWork.StudentEnrollments
                        .HasActiveEnrollmentAsync(currentEnrollment.StudentId, targetAcademicYear.Id, ct);

                    if (hasActiveEnrollment)
                    {
                        await AddFailureAndRollbackAsync(
                            result,
                            currentEnrollment.Id,
                            currentEnrollment.StudentId,
                            currentEnrollment.Student.FullName,
                            "الطالب لديه بالفعل قيد نشط في السنة الدراسية الهدف",
                            ct);
                        continue;
                    }
                }

                currentEnrollment.LeftAt = request.EffectiveDate;
                currentEnrollment.TransferReason = note;
                currentEnrollment.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.StudentEnrollments.Update(currentEnrollment);

                switch (request.Action)
                {
                    case StudentProgressionActionType.Promote:
                    case StudentProgressionActionType.Retain:
                        await _unitOfWork.StudentEnrollments.AddAsync(new StudentEnrollment
                        {
                            StudentId = currentEnrollment.StudentId,
                            ClassId = targetClass!.Id,
                            AcademicYearId = targetAcademicYear!.Id,
                            EnrolledAt = request.EffectiveDate
                        }, ct);

                        result.SuccessCount++;
                        if (request.Action == StudentProgressionActionType.Promote)
                            result.PromotedCount++;
                        else
                            result.RetainedCount++;
                        break;

                    case StudentProgressionActionType.Graduate:
                        currentEnrollment.Student.IsActive = false;
                        currentEnrollment.Student.UpdatedAt = DateTime.UtcNow;
                        _unitOfWork.Students.Update(currentEnrollment.Student);
                        deactivatedStudents.Add(currentEnrollment.Student.FullName);

                        if (currentEnrollment.Student.UserId.HasValue)
                        {
                            var studentUser = await _unitOfWork.Users.GetByIdAsync(currentEnrollment.Student.UserId.Value, ct);
                            if (studentUser is not null && !studentUser.IsDeleted && studentUser.IsActive)
                            {
                                studentUser.IsActive = false;
                                studentUser.UpdatedAt = DateTime.UtcNow;
                                _unitOfWork.Users.Update(studentUser);
                                await _unitOfWork.RefreshTokens.RevokeAllForUserAsync(studentUser.Id, ct);
                            }
                        }

                        await DeactivateParentAccountsIfNeededAsync(
                            currentEnrollment.StudentId,
                            deactivatedParents,
                            ct);

                        result.SuccessCount++;
                        result.GraduatedCount++;
                        break;
                }

                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                AddFailure(
                    result,
                    summary.Id,
                    summary.StudentId,
                    summary.Student.FullName,
                    "حدث خطأ غير متوقع أثناء معالجة الطالب");
            }
        }

        result.DeactivatedStudents = deactivatedStudents.OrderBy(name => name).ToList();
        result.DeactivatedParents = deactivatedParents.OrderBy(name => name).ToList();
        result.FailureCount = result.Failures.Count;

        return OperationResult<StudentProgressionResultDto>.Success(
            result,
            BuildResultMessage(result));
    }

    private async Task DeactivateParentAccountsIfNeededAsync(
        int graduatingStudentId,
        HashSet<string> deactivatedParents,
        CancellationToken ct)
    {
        var parentLinks = await _unitOfWork.ParentStudents.GetWithParentDetailsByStudentAsync(graduatingStudentId, ct);
        foreach (var link in parentLinks.Where(link => !link.IsDeleted))
        {
            var parent = link.Parent;
            if (parent.IsDeleted || !parent.IsActive)
                continue;

            var hasOtherActiveChildren = await ParentHasOtherActiveChildrenAsync(parent.Id, graduatingStudentId, ct);
            if (hasOtherActiveChildren)
                continue;

            parent.IsActive = false;
            parent.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Users.Update(parent);
            await _unitOfWork.RefreshTokens.RevokeAllForUserAsync(parent.Id, ct);
            deactivatedParents.Add(parent.FullName);
        }
    }

    private async Task<bool> ParentHasOtherActiveChildrenAsync(
        int parentId,
        int excludingStudentId,
        CancellationToken ct)
    {
        var siblings = await _unitOfWork.ParentStudents.GetByParentIdAsync(parentId, ct);
        return siblings.Any(link =>
            !link.IsDeleted &&
            link.StudentId != excludingStudentId &&
            link.Student is not null &&
            !link.Student.IsDeleted &&
            link.Student.IsActive);
    }

    private async Task AddFailureAndRollbackAsync(
        StudentProgressionResultDto result,
        int enrollmentId,
        int studentId,
        string studentName,
        string reason,
        CancellationToken ct)
    {
        await _unitOfWork.RollbackTransactionAsync(ct);
        AddFailure(result, enrollmentId, studentId, studentName, reason);
    }

    private static void AddFailure(
        StudentProgressionResultDto result,
        int enrollmentId,
        int studentId,
        string studentName,
        string reason)
    {
        result.Failures.Add(new StudentProgressionFailureDto
        {
            EnrollmentId = enrollmentId,
            StudentId = studentId,
            StudentName = studentName,
            Reason = reason
        });
    }

    private static string BuildResultMessage(StudentProgressionResultDto result)
    {
        if (result.SuccessCount == 0)
            return "لم تنجح أي عملية على الطلاب المحددين";

        if (result.FailureCount == 0)
            return $"تم تنفيذ العملية بنجاح على {result.SuccessCount} طالب";

        return $"تم تنفيذ العملية على {result.SuccessCount} طالب، وتعذر معالجة {result.FailureCount} طالب";
    }
}
