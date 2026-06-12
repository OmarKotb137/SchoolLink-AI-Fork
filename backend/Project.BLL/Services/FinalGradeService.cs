using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.FinalGrades;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class FinalGradeService : IFinalGradeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public FinalGradeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<FinalGradeDto>> CalculateFinalGradeAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted || enrollment.LeftAt is not null)
            return OperationResult<FinalGradeDto>.Failure("القيد غير موجود أو غير نشط");

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(enrollment.ClassId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<FinalGradeDto>.Failure("الفصل غير موجود");

        var template = (await _unitOfWork.EvaluationTemplates.GetByGradeLevelAndYearAsync(
            classEntity.GradeLevelId, enrollment.AcademicYearId)).FirstOrDefault();
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

        var periodAverages = await _unitOfWork.PeriodAverages.GetByEnrollmentIdAsync(enrollmentId);
        if (!periodAverages.Any())
            return OperationResult<FinalGradeDto>.Failure("لم يتم حساب متوسطات الفترات بعد");

        var assessments = await _unitOfWork.PeriodicAssessments.GetByEnrollmentIdAsync(enrollmentId);

        var avgPercentage = periodAverages.Average(a => a.AvgScore);
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

        var existing = await _unitOfWork.FinalGrades.GetByEnrollmentIdAsync(enrollmentId);
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
            await _unitOfWork.FinalGrades.BulkPublishByClassAsync(request.ClassId.Value);
        }
        else
        {
            var classes = await _unitOfWork.Classes.FindAsync(c =>
                c.AcademicYearId == request.AcademicYearId && !c.IsDeleted);
            foreach (var classEntity in classes)
                await _unitOfWork.FinalGrades.BulkPublishByClassAsync(classEntity.Id);
        }

        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم نشر الدرجات بنجاح");
    }

    public async Task<OperationResult<FinalGradeDto>> GetFinalGradeByEnrollmentAsync(int enrollmentId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<FinalGradeDto>.Failure("القيد غير موجود");

        var finalGrade = await _unitOfWork.FinalGrades.GetByEnrollmentIdAsync(enrollmentId);
        if (finalGrade is null || finalGrade.IsDeleted)
            return OperationResult<FinalGradeDto>.Failure("لم يتم حساب الدرجة النهائية بعد");

        return OperationResult<FinalGradeDto>.Success(
            _mapper.Map<FinalGradeDto>(finalGrade),
            "تم جلب الدرجة النهائية بنجاح");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetTopStudentsAsync(int classId, int count)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        var grades = await _unitOfWork.FinalGrades.GetTopStudentsByClassAsync(classId, count);
        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(grades),
            "تم جلب الطلاب المتفوقين بنجاح");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetStudentsNeedingSupportAsync(int classId, decimal threshold)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        var grades = await _unitOfWork.FinalGrades.GetStudentsNeedingSupportAsync(classId, threshold);
        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(grades),
            "تم جلب الطلاب المحتاجين للدعم بنجاح");
    }

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByClassAsync(int classId)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity is null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("الفصل غير موجود");

        var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classId);
        return OperationResult<IEnumerable<FinalGradeDto>>.Success(
            _mapper.Map<IEnumerable<FinalGradeDto>>(grades),
            "تم جلب درجات الفصل بنجاح");
    }

    public async Task<OperationResult<int>> CalculateFinalGradesForClassAsync(int classId)
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
            var result = await CalculateFinalGradeAsync(enrollment.Id);
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
        foreach (var s in request.Students)
        {
            if ((s.MonthlyExam1Score ?? 0) > 0)
                assessments.Add(new PeriodicAssessment
                {
                    EnrollmentId = s.EnrollmentId,
                    AssessmentType = PeriodicAssessmentType.MonthlyExam1,
                    Score = s.MonthlyExam1Score.Value,
                    MaxScore = 15,
                    AssessmentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            if ((s.MonthlyExam2Score ?? 0) > 0)
                assessments.Add(new PeriodicAssessment
                {
                    EnrollmentId = s.EnrollmentId,
                    AssessmentType = PeriodicAssessmentType.MonthlyExam2,
                    Score = s.MonthlyExam2Score.Value,
                    MaxScore = 15,
                    AssessmentDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            if ((s.SemesterExamScore ?? 0) > 0)
                assessments.Add(new PeriodicAssessment
                {
                    EnrollmentId = s.EnrollmentId,
                    AssessmentType = PeriodicAssessmentType.SemesterExam,
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

    public async Task<OperationResult<IEnumerable<FinalGradeDto>>> GetFinalGradesByAcademicYearAsync(int academicYearId)
    {
        var year = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId);
        if (year is null || year.IsDeleted)
            return OperationResult<IEnumerable<FinalGradeDto>>.Failure("السنة الدراسية غير موجودة");

        var classes = await _unitOfWork.Classes.FindAsync(c =>
            c.AcademicYearId == academicYearId && !c.IsDeleted);

        var allGrades = new List<FinalGrade>();
        foreach (var classEntity in classes)
        {
            var grades = await _unitOfWork.FinalGrades.GetByClassIdAsync(classEntity.Id);
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
