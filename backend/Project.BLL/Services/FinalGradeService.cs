using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.FinalGrades;
using Project.BLL.DTOs.Notifications;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class FinalGradeService : IFinalGradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;
    private readonly INotificationService _notificationService;

    public FinalGradeService(IUnitOfWork unitOfWork, IMapper mapper, INotificationService notificationService)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
        _notificationService = notificationService;
    }

    public async Task<OperationResult<FinalGradeDto>> CalculateFinalGradeAsync(int enrollmentId, AcademicTerm? term = null)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted || enrollment.LeftAt is not null)
            return OperationResult<FinalGradeDto>.Failure("القيد غير موجود أو غير نشط");

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(enrollment.ClassId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<FinalGradeDto>.Failure("الفصل غير موجود");

        var template = (await _unitOfWork.EvaluationTemplates.GetByGradeLevelAndYearAsync(
            classEntity.GradeLevelId, enrollment.AcademicYearId, term)).FirstOrDefault();
        if (template is null)
            return OperationResult<FinalGradeDto>.Failure("لم يتم العثور على قالب تقييم لهذا الصف");

        var templateWithItems = await _unitOfWork.EvaluationTemplates.GetWithItemsAsync(template.Id);
        if (templateWithItems is null)
            return OperationResult<FinalGradeDto>.Failure("لم يتم العثور على عناصر القالب");

        var yearWorkMax = templateWithItems.Items
            .Where(i => !i.IsDeleted)
            .Sum(i => i.MaxScore * i.Weight);
        if (yearWorkMax == 0)
            return OperationResult<FinalGradeDto>.Failure("مجموع درجات القالب يساوي صفر");

        var resolvedTerm = term ?? (template.Term.HasValue ? template.Term.Value : AcademicTerm.FirstSemester);

        // Get semester weeks to filter period averages
        IReadOnlyList<EvaluationPeriod> semesterWeeks;
        if (template.Term.HasValue)
        {
            semesterWeeks = await _unitOfWork.EvaluationPeriods.GetWeeksByYearAndSemesterAsync(
                enrollment.AcademicYearId, (int)template.Term.Value);
        }
        else
        {
            semesterWeeks = await _unitOfWork.EvaluationPeriods.GetWeeksByYearAsync(enrollment.AcademicYearId);
        }

        var semesterPeriodIds = semesterWeeks.Select(p => p.Id).ToHashSet();

        var periodAverages = await _unitOfWork.PeriodAverages.GetByEnrollmentIdAsync(enrollmentId);
        var filteredAverages = periodAverages.Where(pa => semesterPeriodIds.Contains(pa.PeriodId)).ToList();

        if (!filteredAverages.Any())
            return OperationResult<FinalGradeDto>.Failure("لم يتم حساب متوسطات الفترات بعد");

        var assessments = await _unitOfWork.PeriodicAssessments.GetByEnrollmentAndTypesAsync(
            enrollmentId, new[] {
                PeriodicAssessmentType.MonthlyExam1,
                PeriodicAssessmentType.MonthlyExam2,
                PeriodicAssessmentType.SemesterExam,
                PeriodicAssessmentType.InitialAssessment,
                PeriodicAssessmentType.FinalAssessment
            });

        // Filter assessments by term if applicable
        assessments = assessments.Where(a => a.Term == null || a.Term == resolvedTerm).ToList();

        var avgPercentage = filteredAverages.Average(a => a.AvgScore);
        var periodAvgScore = RoundGrade(avgPercentage * yearWorkMax / 100m);
        var assessment1 = assessments.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam1
                                                       || a.AssessmentType == PeriodicAssessmentType.InitialAssessment);
        var assessment2 = assessments.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam2
                                                       || a.AssessmentType == PeriodicAssessmentType.FinalAssessment);

        var finalExamAssessment = assessments.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.SemesterExam);
        var finalExamScore = finalExamAssessment?.Score ?? 0;
        var writtenTotal = RoundGrade(periodAvgScore + (assessment1?.Score ?? 0) + (assessment2?.Score ?? 0));
        var total = RoundGrade(writtenTotal + finalExamScore);
        var isComplete = (assessment1?.Score ?? 0) > 0 && (assessment2?.Score ?? 0) > 0 && (finalExamAssessment?.Score ?? 0) > 0;
        var maxTotal = yearWorkMax;
        if ((assessment1?.Score ?? 0) > 0) maxTotal += assessment1!.MaxScore;
        if ((assessment2?.Score ?? 0) > 0) maxTotal += assessment2!.MaxScore;
        if ((finalExamAssessment?.Score ?? 0) > 0) maxTotal += finalExamAssessment!.MaxScore;

        var existing = await _unitOfWork.FinalGrades.GetByEnrollmentIdAsync(enrollmentId, resolvedTerm);
        if (existing is not null && !existing.IsDeleted)
        {
            existing.PeriodAvgScore = periodAvgScore;
            existing.Assessment1Score = assessment1?.Score ?? 0;
            existing.Assessment2Score = assessment2?.Score ?? 0;
            existing.WrittenTotal = writtenTotal;
            existing.FinalExamScore = finalExamScore;
            existing.Total = total;
            existing.MaxTotal = maxTotal;
            existing.IsComplete = isComplete;
            _unitOfWork.FinalGrades.Update(existing);
        }
        else
        {
            existing = new FinalGrade
            {
                EnrollmentId = enrollmentId,
                Term = resolvedTerm,
                PeriodAvgScore = periodAvgScore,
                Assessment1Score = assessment1?.Score ?? 0,
                Assessment2Score = assessment2?.Score ?? 0,
                WrittenTotal = writtenTotal,
                FinalExamScore = finalExamScore,
                Total = total,
                MaxTotal = maxTotal,
                IsPublished = false,
                IsComplete = isComplete
            };
            await _unitOfWork.FinalGrades.AddAsync(existing);
        }

        await _unitOfWork.SaveChangesAsync();

        return OperationResult<FinalGradeDto>.Success(
            _mapper.Map<FinalGradeDto>(existing),
            "تم حساب الدرجة النهائية بنجاح");
    }

    public async Task<OperationResult> PublishGradesAsync(PublishGradesRequest request)
    {
        var admin = await _unitOfWork.Users.GetByIdAsync(request.PublishedById);
        if (admin is null || admin.IsDeleted || !admin.IsActive || !admin.Role.IsAdminLike())
            return OperationResult.Failure("يجب أن يكون المستخدم مسؤولاً لنشر الدرجات");

        var year = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (year is null || year.IsDeleted)
            return OperationResult.Failure("السنة الدراسية غير موجودة");

        if (request.ClassId.HasValue)
        {
            await _unitOfWork.FinalGrades.BulkPublishByClassAsync(request.ClassId.Value, request.Term);
        }
        else
        {
            var classes = await _unitOfWork.Classes.FindAsync(c =>
                c.AcademicYearId == request.AcademicYearId && !c.IsDeleted);
            foreach (var classEntity in classes)
                await _unitOfWork.FinalGrades.BulkPublishByClassAsync(classEntity.Id, request.Term);
        }

        await _unitOfWork.SaveChangesAsync();

        // إشعار بنشر الدرجات للطلاب وأولياء الأمور
        var affectedEnrollments = new List<StudentEnrollment>();
        if (request.ClassId.HasValue)
        {
            var classEntity = await _unitOfWork.Classes.GetByIdAsync(request.ClassId.Value);
            if (classEntity != null)
            {
                var enrollments = await _unitOfWork.StudentEnrollments
                    .GetActiveByClassAsync(request.ClassId.Value, request.AcademicYearId);
                affectedEnrollments.AddRange(enrollments);
            }
        }
        else
        {
            var classes = await _unitOfWork.Classes.FindAsync(c =>
                c.AcademicYearId == request.AcademicYearId && !c.IsDeleted);
            foreach (var c in classes)
            {
                var enrollments = await _unitOfWork.StudentEnrollments
                    .GetActiveByClassAsync(c.Id, request.AcademicYearId);
                affectedEnrollments.AddRange(enrollments);
            }
        }

        var allRecipients = new List<int>();
        foreach (var enrollment in affectedEnrollments.DistinctBy(e => e.StudentId))
        {
            var student = await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId);
            if (student?.UserId != null)
                allRecipients.Add(student.UserId.Value);

            var parentUsers = await _unitOfWork.ParentStudents
                .FindAsync(ps => ps.StudentId == enrollment.StudentId);
            allRecipients.AddRange(parentUsers.Select(p => p.ParentId));
        }

        if (allRecipients.Count != 0)
        {
            await _notificationService.SendBulkNotificationAsync(new SendBulkNotificationRequest
            {
                UserIds = allRecipients.Distinct().ToList(),
                Title = "نشر الدرجات النهائية",
                Body = $"تم نشر الدرجات النهائية للفصل الدراسي {(request.Term == AcademicTerm.FirstSemester ? "الأول" : "الثاني")}",
                Type = NotificationType.GradePublished
            });
        }

        // Threshold check: إشعار للطلاب الذين تقل درجاتهم عن 50%
        const decimal thresholdPercent = 0.5m;
        var classesToCheck = request.ClassId.HasValue
            ? new[] { request.ClassId.Value }
            : (await _unitOfWork.Classes.FindAsync(c =>
                c.AcademicYearId == request.AcademicYearId && !c.IsDeleted)).Select(c => c.Id);

        foreach (var classId in classesToCheck)
        {
            var classGrades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classId, request.Term);
            var belowThreshold = classGrades
                .Where(g => !g.IsDeleted && g.MaxTotal > 0 && (decimal)g.Total / g.MaxTotal < thresholdPercent)
                .ToList();

            foreach (var grade in belowThreshold)
            {
                var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(grade.EnrollmentId);
                if (enrollment == null) continue;

                var student = await _unitOfWork.Students.GetByIdAsync(enrollment.StudentId);
                var studentName = student?.FullName ?? "طالب";

                var parentUsers = await _unitOfWork.ParentStudents
                    .FindAsync(ps => ps.StudentId == enrollment.StudentId);
                var userRecipients = new List<int>();
                if (student?.UserId != null)
                    userRecipients.Add(student.UserId.Value);
                userRecipients.AddRange(parentUsers.Select(p => p.ParentId));

                if (userRecipients.Count == 0) continue;

                // GradeThresholdAlert
                await _notificationService.SendBulkNotificationAsync(new SendBulkNotificationRequest
                {
                    UserIds = userRecipients.Distinct().ToList(),
                    Title = "تنبيه تدني درجات",
                    Body = $"درجات الطالب {studentName} أقل من 50% في الفصل الدراسي {(request.Term == AcademicTerm.FirstSemester ? "الأول" : "الثاني")}",
                    Type = NotificationType.GradeThresholdAlert
                });

                // AcademicProbation
                await _notificationService.SendBulkNotificationAsync(new SendBulkNotificationRequest
                {
                    UserIds = userRecipients.Distinct().ToList(),
                    Title = "إنذار أكاديمي",
                    Body = $"الطالب {studentName} تحت الملاحظة الأكاديمية بسبب تدني الدرجات",
                    Type = NotificationType.AcademicProbation
                });
            }
        }

        return OperationResult.Success("تم نشر الدرجات بنجاح");
    }

    public async Task<OperationResult<FinalGradeDto>> GetFinalGradeByEnrollmentAsync(int enrollmentId, AcademicTerm? term = null)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<FinalGradeDto>.Failure("القيد غير موجود");

        var finalGrade = await _unitOfWork.FinalGrades.GetByEnrollmentIdAsync(enrollmentId, term);
        if (finalGrade is null || finalGrade.IsDeleted)
            return OperationResult<FinalGradeDto>.Failure("لم يتم حساب الدرجة النهائية بعد");

        return OperationResult<FinalGradeDto>.Success(
            _mapper.Map<FinalGradeDto>(finalGrade),
            "تم جلب الدرجة النهائية بنجاح");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetTopStudentsAsync(int classId, int count, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        var grades = await _unitOfWork.FinalGrades.GetTopStudentsByClassAsync(classId, count, term);
        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(grades),
            "تم جلب الطلاب المتفوقين بنجاح");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetStudentsNeedingSupportAsync(int classId, decimal threshold, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        var grades = await _unitOfWork.FinalGrades.GetStudentsNeedingSupportAsync(classId, threshold, term);
        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(grades),
            "تم جلب الطلاب المحتاجين للدعم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByClassAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classId, term);
        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(grades),
            "تم جلب درجات الفصل بنجاح");
    }

    public async Task<OperationResult<int>> CalculateFinalGradesForClassAsync(int classId, AcademicTerm? term = null)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<int>.Failure("الفصل غير موجود");

        var enrollments = await _unitOfWork.StudentEnrollments
            .GetActiveByClassAsync(classId, classEntity.AcademicYearId);

        if (!enrollments.Any())
            return OperationResult<int>.Success(0, "لا يوجد طلاب في هذا الفصل");

        var calculated = 0;
        foreach (var enrollment in enrollments)
        {
            var result = await CalculateFinalGradeAsync(enrollment.Id, term);
            if (result.IsSuccess)
                calculated++;
        }

        return OperationResult<int>.Success(calculated, $"تم حساب الدرجة النهائية لـ {calculated} طالب بنجاح");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> CalculateFullForClassAsync(int classId, CalculateFullFinalGradesRequest request)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        var assessments = new List<PeriodicAssessment>();
        var term = request.Term ?? AcademicTerm.FirstSemester;
        foreach (var s in request.Students)
        {
            if ((s.MonthlyExam1Score ?? 0) > 0)
                assessments.Add(new PeriodicAssessment
                {
                    EnrollmentId = s.EnrollmentId,
                    AssessmentType = PeriodicAssessmentType.MonthlyExam1,
                    Term = term,
                    Score = s.MonthlyExam1Score.Value,
                    MaxScore = 15,
                    AssessmentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            if ((s.MonthlyExam2Score ?? 0) > 0)
                assessments.Add(new PeriodicAssessment
                {
                    EnrollmentId = s.EnrollmentId,
                    AssessmentType = PeriodicAssessmentType.MonthlyExam2,
                    Term = term,
                    Score = s.MonthlyExam2Score.Value,
                    MaxScore = 15,
                    AssessmentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            if ((s.SemesterExamScore ?? 0) > 0)
                assessments.Add(new PeriodicAssessment
                {
                    EnrollmentId = s.EnrollmentId,
                    AssessmentType = PeriodicAssessmentType.SemesterExam,
                    Term = term,
                    Score = s.SemesterExamScore.Value,
                    MaxScore = 30,
                    AssessmentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
        }

        if (assessments.Any())
            await _unitOfWork.PeriodicAssessments.BulkUpsertAsync(assessments);

        await _unitOfWork.SaveChangesAsync();

        return await RecalculateAllAsync(classId);
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> RecalculateForClassAsync(int classId)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        return await RecalculateAllAsync(classId);
    }

    private async Task<OperationResult<IEnumerable<FinalGradeDto>>> RecalculateAllAsync(int classId)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        var weeklyPeriods = await _unitOfWork.EvaluationPeriods.GetWeeksByYearAsync(classEntity.AcademicYearId);
        var enrollments = await _unitOfWork.StudentEnrollments.GetActiveByClassAsync(classId, classEntity.AcademicYearId);

        if (!enrollments.Any())
            return OperationResult<IEnumerable<FinalGradeDto>>.Success(Array.Empty<FinalGradeDto>(), "لا يوجد طلاب في هذا الفصل");

        foreach (var period in weeklyPeriods)
        {
            foreach (var enrollment in enrollments)
            {
                var evaluations = await _unitOfWork.StudentEvaluations.GetByEnrollmentAndPeriodAsync(enrollment.Id, period.Id);
                if (!evaluations.Any()) continue;

                var totalScore = evaluations.Sum(e => e.Score ?? 0);
                var itemIds = evaluations.Select(e => e.EvaluationItemId).Distinct().ToList();
                var evaluatedItems = await _unitOfWork.EvaluationItems.FindAsync(i => itemIds.Contains(i.Id));
                var totalMax = evaluatedItems.Where(i => !i.IsDeleted).Sum(i => i.MaxScore * i.Weight);
                var avgScore = totalMax > 0 ? (totalScore / totalMax) * 100 : 0;

                var existing = await _unitOfWork.PeriodAverages.GetByEnrollmentAndPeriodAsync(enrollment.Id, period.Id);
                if (existing is not null && !existing.IsDeleted)
                {
                    existing.AvgScore = avgScore;
                    existing.MaxScore = totalMax;
                    existing.CalculatedAt = DateTime.UtcNow;
                    _unitOfWork.PeriodAverages.Update(existing);
                }
                else
                {
                    await _unitOfWork.PeriodAverages.AddAsync(new PeriodAverage
                    {
                        EnrollmentId = enrollment.Id,
                        PeriodId = period.Id,
                        AvgScore = avgScore,
                        MaxScore = totalMax,
                        CalculatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();

        var calculated = 0;
        foreach (var enrollment in enrollments)
        {
            var result = await CalculateFinalGradeAsync(enrollment.Id);
            if (result.IsSuccess)
                calculated++;
        }

        var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classId);
        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(grades),
            $"تم حساب النتائج النهائية لـ {calculated} طالب");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByAcademicYearAsync(int academicYearId, AcademicTerm? term = null)
    {
        var year = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId);
        if (year is null || year.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("السنة الدراسية غير موجودة");

        var classes = await _unitOfWork.Classes.FindAsync(c =>
            c.AcademicYearId == academicYearId && !c.IsDeleted);

        var allGrades = new List<FinalGrade>();
        foreach (var classEntity in classes)
        {
            var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classEntity.Id, term);
            allGrades.AddRange(grades);
        }

        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(allGrades),
            "تم جلب الدرجات النهائية للسنة الدراسية بنجاح");
    }

    private static decimal RoundGrade(decimal value)
    {
        var integer = Math.Floor(value);
        var fraction = value - integer;

        if (fraction >= 0.5m)
            return Math.Ceiling(value);
        if (fraction > 0)
            return integer + 0.5m;
        return integer;
    }
}
