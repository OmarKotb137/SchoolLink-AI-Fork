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

        var templates = await _unitOfWork.EvaluationTemplates.GetByGradeLevelAndYearAsync(
            classEntity.GradeLevelId, enrollment.AcademicYearId);
        if (!templates.Any())
            return OperationResult<FinalGradeDto>.Failure("لم يتم العثور على قالب تقييم لهذا الصف");

        var periodAverages = await _unitOfWork.PeriodAverages.GetByEnrollmentIdAsync(enrollmentId);
        if (!periodAverages.Any())
            return OperationResult<FinalGradeDto>.Failure("لم يتم حساب متوسطات الفترات بعد");

        var assessments = await _unitOfWork.PeriodicAssessments.GetByEnrollmentIdAsync(enrollmentId);

        var periodAvgScore = periodAverages.Average(a => a.AvgScore);
        var assessment1 = assessments.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam1
                                                       || a.AssessmentType == PeriodicAssessmentType.InitialAssessment);
        var assessment2 = assessments.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.MonthlyExam2
                                                       || a.AssessmentType == PeriodicAssessmentType.FinalAssessment);

        var writtenTotal = periodAvgScore + (assessment1?.Score ?? 0) + (assessment2?.Score ?? 0);
        var finalExamScore = assessments.FirstOrDefault(a => a.AssessmentType == PeriodicAssessmentType.SemesterExam)?.Score ?? 0;
        var total = writtenTotal + finalExamScore;

        var existing = await _unitOfWork.FinalGrades.GetByEnrollmentIdAsync(enrollmentId);
        if (existing is not null && !existing.IsDeleted)
        {
            existing.PeriodAvgScore = periodAvgScore;
            existing.Assessment1Score = assessment1?.Score ?? 0;
            existing.Assessment2Score = assessment2?.Score ?? 0;
            existing.WrittenTotal = writtenTotal;
            existing.FinalExamScore = finalExamScore;
            existing.Total = total;
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
                IsPublished = false
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
        if (admin is null || admin.IsDeleted || !admin.IsActive || admin.Role != UserRole.Admin)
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
}
