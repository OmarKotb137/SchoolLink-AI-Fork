using AutoMapper;
using Common.Results;
using Project.BLL.DTOs.Enrollments;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.Services;

public class StudentEnrollmentService : IStudentEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public StudentEnrollmentService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OperationResult<EnrollmentDto>> EnrollStudentAsync(EnrollStudentRequest request)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(request.StudentId);
        if (student == null || student.IsDeleted)
            return OperationResult<EnrollmentDto>.Failure("الطالب غير موجود");

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(request.ClassId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<EnrollmentDto>.Failure("الفصل غير موجود");

        var year = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (year == null || year.IsDeleted)
            return OperationResult<EnrollmentDto>.Failure("السنة الدراسية غير موجودة");

        if (request.EnrolledAt > DateOnly.FromDateTime(DateTime.UtcNow))
            return OperationResult<EnrollmentDto>.Failure("تاريخ التسجيل لا يمكن أن يكون في المستقبل");

        var existingEnrollments = await _unitOfWork.StudentEnrollments.GetHistoryByStudentAsync(request.StudentId);
        if (existingEnrollments.Any(e => !e.IsDeleted && e.LeftAt == null
            && e.ClassId == request.ClassId && e.AcademicYearId == request.AcademicYearId))
            return OperationResult<EnrollmentDto>.Failure("الطالب مسجل بالفعل في هذا الفصل لنفس السنة الدراسية");

        var enrollment = _mapper.Map<StudentEnrollment>(request);
        await _unitOfWork.StudentEnrollments.AddAsync(enrollment);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<EnrollmentDto>(enrollment);
        return OperationResult<EnrollmentDto>.Success(dto, "تم تسجيل الطالب في الفصل بنجاح");
    }

    public async Task<OperationResult<EnrollmentDto>> TransferStudentAsync(TransferStudentRequest request)
    {
        var currentEnrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(request.CurrentEnrollmentId);
        if (currentEnrollment == null || currentEnrollment.IsDeleted)
            return OperationResult<EnrollmentDto>.Failure("التسجيل الحالي غير موجود");

        if (currentEnrollment.LeftAt != null)
            return OperationResult<EnrollmentDto>.Failure("التسجيل الحالي مغلق بالفعل");

        var newClass = await _unitOfWork.Classes.GetByIdAsync(request.NewClassId);
        if (newClass == null || newClass.IsDeleted)
            return OperationResult<EnrollmentDto>.Failure("الفصل الجديد غير موجود");

        var existingEnrollments = await _unitOfWork.StudentEnrollments.GetHistoryByStudentAsync(currentEnrollment.StudentId);
        if (existingEnrollments.Any(e => !e.IsDeleted && e.LeftAt == null
            && e.ClassId == request.NewClassId && e.AcademicYearId == currentEnrollment.AcademicYearId))
            return OperationResult<EnrollmentDto>.Failure("الطالب مسجل بالفعل في الفصل الجديد");

        currentEnrollment.LeftAt = request.TransferDate;
        currentEnrollment.TransferReason = request.TransferReason;
        _unitOfWork.StudentEnrollments.Update(currentEnrollment);

        var newEnrollment = new StudentEnrollment
        {
            StudentId = currentEnrollment.StudentId,
            ClassId = request.NewClassId,
            AcademicYearId = currentEnrollment.AcademicYearId,
            EnrolledAt = request.TransferDate
        };

        await _unitOfWork.StudentEnrollments.AddAsync(newEnrollment);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<EnrollmentDto>(newEnrollment);
        return OperationResult<EnrollmentDto>.Success(dto, "تم نقل الطالب إلى الفصل الجديد بنجاح");
    }

    public async Task<OperationResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByStudentAsync(int studentId)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<IEnumerable<EnrollmentDto>>.Failure("الطالب غير موجود");

        var enrollments = await _unitOfWork.StudentEnrollments.GetHistoryByStudentAsync(studentId);
        var ordered = enrollments.Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.EnrolledAt)
            .ToList();

        var dtos = _mapper.Map<IEnumerable<EnrollmentDto>>(ordered);
        foreach (var dto in dtos)
            dto.StudentName = student.FullName;

        return OperationResult<IEnumerable<EnrollmentDto>>.Success(dtos);
    }

    public async Task<OperationResult<IEnumerable<EnrollmentDto>>> GetEnrollmentsByClassAsync(int classId, int academicYearId, bool activeOnly)
    {
        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<IEnumerable<EnrollmentDto>>.Failure("الفصل غير موجود");

        var year = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId);
        if (year == null || year.IsDeleted)
            return OperationResult<IEnumerable<EnrollmentDto>>.Failure("السنة الدراسية غير موجودة");

        IReadOnlyList<StudentEnrollment> enrollments;

        if (activeOnly)
            enrollments = await _unitOfWork.StudentEnrollments.GetByClassWithStudentAsync(classId, academicYearId);
        else
            enrollments = await _unitOfWork.StudentEnrollments.GetByClassAndYearAsync(classId, academicYearId);

        var filtered = enrollments.Where(e => !e.IsDeleted).ToList();
        var dtos = _mapper.Map<IEnumerable<EnrollmentDto>>(filtered);
        foreach (var dto in dtos)
        {
            dto.ClassName = classEntity.Name;
            dto.AcademicYearName = year.Name;
        }

        return OperationResult<IEnumerable<EnrollmentDto>>.Success(dtos);
    }

    public async Task<OperationResult<EnrollmentDto>> GetActiveEnrollmentByStudentAsync(int studentId, int academicYearId)
    {
        var student = await _unitOfWork.Students.GetByIdAsync(studentId);
        if (student == null || student.IsDeleted)
            return OperationResult<EnrollmentDto>.Failure("الطالب غير موجود");

        var enrollment = await _unitOfWork.StudentEnrollments.GetActiveByStudentAndYearAsync(studentId, academicYearId);
        if (enrollment == null || enrollment.IsDeleted)
            return OperationResult<EnrollmentDto>.Failure("لا يوجد تسجيل نشط للطالب في هذه السنة الدراسية");

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(enrollment.ClassId);
        var year = await _unitOfWork.AcademicYears.GetByIdAsync(enrollment.AcademicYearId);

        var dto = _mapper.Map<EnrollmentDto>(enrollment);
        dto.StudentName = student.FullName;
        dto.ClassName = classEntity?.Name ?? string.Empty;
        dto.AcademicYearName = year?.Name ?? string.Empty;

        return OperationResult<EnrollmentDto>.Success(dto);
    }

    public async Task<OperationResult<IEnumerable<TransferHistoryDto>>> GetTransferHistoryAsync(int academicYearId)
    {
        var transfers = await _unitOfWork.StudentEnrollments.GetTransfersHistoryAsync(academicYearId);
        
        var dtos = transfers.Select(e => new TransferHistoryDto
        {
            Id = e.Id,
            StudentName = e.Student?.FullName ?? string.Empty,
            FromClass = e.Class?.Name ?? string.Empty,
            // To find the "ToClass", we'd need to look at the next enrollment for the same student.
            // But since the current enrollment tracks the destination class? Wait.
            // In TransferStudentAsync: 
            // currentEnrollment.LeftAt = request.TransferDate; currentEnrollment.TransferReason = request.TransferReason; _unitOfWork.Update(currentEnrollment);
            // var newEnrollment = new StudentEnrollment { ClassId = request.NewClassId ... }
            // This means `e.Class.Name` is the OLD class. We don't have the NEW class directly on the closed enrollment.
            // A quick way is to set ToClass = "تم النقل", or we fetch their active enrollment.
            // Let's resolve the next enrollment.
        }).ToList();

        // Let's properly fill ToClass by looking up their next active enrollment for this year.
        foreach (var dto in dtos)
        {
            var nextEnrollment = await _unitOfWork.StudentEnrollments.GetActiveByStudentAndYearAsync(transfers.First(t => t.Id == dto.Id).StudentId, academicYearId);
            if (nextEnrollment != null)
            {
                var nextClass = await _unitOfWork.Classes.GetByIdAsync(nextEnrollment.ClassId);
                dto.ToClass = nextClass?.Name ?? "غير معروف";
            }
            var original = transfers.First(t => t.Id == dto.Id);
            dto.TransferDate = original.LeftAt;
            dto.Reason = original.TransferReason;
        }

        return OperationResult<IEnumerable<TransferHistoryDto>>.Success(dtos);
    }

    public async Task<OperationResult<BulkEnrollResultDto>> BulkEnrollStudentsAsync(BulkEnrollStudentsRequest request)
    {
        if (request.StudentIds == null || request.StudentIds.Count == 0)
            return OperationResult<BulkEnrollResultDto>.Failure("يجب توفير طالب واحد على الأقل");

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(request.ClassId);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<BulkEnrollResultDto>.Failure("الفصل غير موجود");

        var year = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (year == null || year.IsDeleted)
            return OperationResult<BulkEnrollResultDto>.Failure("السنة الدراسية غير موجودة");

        if (request.EnrolledAt > DateOnly.FromDateTime(DateTime.UtcNow))
            return OperationResult<BulkEnrollResultDto>.Failure("تاريخ التسجيل لا يمكن أن يكون في المستقبل");

        var result = new BulkEnrollResultDto { TotalRequested = request.StudentIds.Count };

        foreach (var studentId in request.StudentIds)
        {
            var student = await _unitOfWork.Students.GetByIdAsync(studentId);
            if (student == null || student.IsDeleted)
            {
                result.FailureCount++;
                result.Failures.Add(new BulkEnrollFailureDto
                {
                    StudentId = studentId,
                    StudentName = "غير معروف",
                    Reason = "الطالب غير موجود"
                });
                continue;
            }

            var existingEnrollments = await _unitOfWork.StudentEnrollments.GetHistoryByStudentAsync(studentId);
            if (existingEnrollments.Any(e => !e.IsDeleted && e.LeftAt == null
                && e.ClassId == request.ClassId && e.AcademicYearId == request.AcademicYearId))
            {
                result.FailureCount++;
                result.Failures.Add(new BulkEnrollFailureDto
                {
                    StudentId = studentId,
                    StudentName = student.FullName,
                    Reason = "الطالب مسجل بالفعل في هذا الفصل"
                });
                continue;
            }

            var enrollment = new StudentEnrollment
            {
                StudentId = studentId,
                ClassId = request.ClassId,
                AcademicYearId = request.AcademicYearId,
                EnrolledAt = request.EnrolledAt
            };

            await _unitOfWork.StudentEnrollments.AddAsync(enrollment);
            result.SuccessCount++;
        }

        await _unitOfWork.SaveChangesAsync();
        return OperationResult<BulkEnrollResultDto>.Success(result,
            $"تم تسجيل {result.SuccessCount} من {result.TotalRequested} طالب بنجاح");
    }
}
