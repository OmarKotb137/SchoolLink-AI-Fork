using AutoMapper;
using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;
using Project.DAL.Interfaces;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services;

public class TimetableService : ITimetableService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;

    public TimetableService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }

    public async Task<OperationResult<TimetableDto>> CreateTimetableAsync(
        CreateTimetableRequest request)
    {
        // 1. Validate Class
        var schoolClass = await _unitOfWork.Classes.GetByIdAsync(request.ClassId);
        if (schoolClass is null || schoolClass.IsDeleted)
            return OperationResult<TimetableDto>.Failure("الفصل غير موجود");

        // 2. Validate AcademicYear
        var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(request.AcademicYearId);
        if (academicYear is null || academicYear.IsDeleted)
            return OperationResult<TimetableDto>.Failure("السنة الدراسية غير موجودة");

        // 3. Create entity with IsActive explicitly set to false
        var entity = new Timetable
        {
            ClassId        = request.ClassId,
            AcademicYearId = request.AcademicYearId,
            IsActive       = false
        };

        // 4. Persist
        await _unitOfWork.Timetables.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        // 5. Reload with Class navigation; slots are expected to be empty for a new draft
        var withClass = await _unitOfWork.Timetables.GetWithClassAndAllSlotsAsync(entity.Id);

        return OperationResult<TimetableDto>.Success(
            _mapper.Map<TimetableDto>(withClass),
            "تم إنشاء الجدول الدراسي بنجاح");
    }

    public async Task<OperationResult<TimetableDto>> GetTimetableByIdAsync(int timetableId)
    {
        var timetable = await _unitOfWork.Timetables.GetWithClassAndAllSlotsAsync(timetableId);
        if (timetable is null || timetable.IsDeleted)
            return OperationResult<TimetableDto>.Failure("الجدول الدراسي غير موجود");

        return OperationResult<TimetableDto>.Success(
            _mapper.Map<TimetableDto>(timetable),
            "تم جلب الجدول الدراسي بنجاح");
    }

    public async Task<OperationResult> ActivateTimetableAsync(int timetableId)
    {
        // 1. Find timetable
        var timetable = await _unitOfWork.Timetables.GetByIdAsync(timetableId);
        if (timetable is null || timetable.IsDeleted)
            return OperationResult.Failure("الجدول الدراسي غير موجود");

        // 2. Must have at least one non-deleted slot
        var hasSlots = await _unitOfWork.TimetableSlots.AnyAsync(
            s => s.TimetableId == timetableId && !s.IsDeleted);
        if (!hasSlots)
            return OperationResult.Failure("لا يمكن تفعيل جدول دراسي بدون حصص");

        // 3. Rooms are reserved only on activation, so validate the draft now.
        var draftSlots = await _unitOfWork.TimetableSlots.GetByTimetableIdAsync(timetableId);
        foreach (var slot in draftSlots.Where(s => s.RoomId.HasValue))
        {
            if (await _unitOfWork.TimetableSlots.HasRoomConflictAgainstActiveTimetablesAsync(
                    slot.RoomId!.Value,
                    slot.DayOfWeek,
                    slot.PeriodNumber,
                    timetableId))
            {
                return OperationResult.Failure(
                    $"لا يمكن تفعيل الجدول لأن الغرفة '{slot.Room?.Name ?? slot.RoomId.Value.ToString()}' محجوزة في نفس اليوم ونفس الحصة");
            }
        }

        // 4. Transaction: deactivate current timetable, then activate this one
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.Timetables.DeactivateByClassAndYearAsync(
                timetable.ClassId, timetable.AcademicYearId);

            timetable.IsActive  = true;
            timetable.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Timetables.Update(timetable);
            await _unitOfWork.SaveChangesAsync();
        });

        return OperationResult.Success("تم تفعيل الجدول الدراسي بنجاح");
    }

    public async Task<OperationResult> DeactivateTimetableAsync(int timetableId)
    {
        var timetable = await _unitOfWork.Timetables.GetByIdAsync(timetableId);
        if (timetable is null || timetable.IsDeleted)
            return OperationResult.Failure("الجدول الدراسي غير موجود");

        if (!timetable.IsActive)
            return OperationResult.Success("الجدول الدراسي غير مفعل بالفعل");

        timetable.IsActive  = false;
        timetable.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Timetables.Update(timetable);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم إلغاء تفعيل الجدول الدراسي بنجاح");
    }

    public async Task<OperationResult> DeleteTimetableAsync(int timetableId)
    {
        // Active timetables must be deactivated first to avoid removing the live schedule.
        var timetable = await _unitOfWork.Timetables.GetByIdAsync(timetableId);
        if (timetable is null || timetable.IsDeleted)
            return OperationResult.Failure("الجدول الدراسي غير موجود");

        if (timetable.IsActive)
            return OperationResult.Failure("لا يمكن حذف جدول دراسي مفعل، يجب إلغاء تفعيله أولا");

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var slots = await _unitOfWork.TimetableSlots.GetByTimetableIdAsync(timetableId);
            _unitOfWork.TimetableSlots.SoftDeleteRange(slots);
            _unitOfWork.Timetables.SoftDelete(timetable);
            await _unitOfWork.SaveChangesAsync();
        });

        return OperationResult.Success("تم حذف الجدول الدراسي بنجاح");
    }

    public async Task<OperationResult<TimetableSlotDto>> AddTimetableSlotAsync(
        AddTimetableSlotRequest request)
    {
        // 1. Validate Timetable
        var timetable = await _unitOfWork.Timetables.GetByIdAsync(request.TimetableId);
        if (timetable is null || timetable.IsDeleted)
            return OperationResult<TimetableSlotDto>.Failure("الجدول الدراسي غير موجود");

        // 2. Day + Period uniqueness
        if (await _unitOfWork.TimetableSlots.HasConflictAsync(
                request.TimetableId, request.DayOfWeek, request.PeriodNumber))
            return OperationResult<TimetableSlotDto>.Failure(
                "توجد حصة بالفعل في هذا اليوم وهذا الرقم");

        // 3. Time range validation
        if (request.EndTime <= request.StartTime)
            return OperationResult<TimetableSlotDto>.Failure(
                "وقت النهاية يجب أن يكون بعد وقت البداية");

        // 4. Non-break validation
        if (!request.IsBreak)
        {
            if (request.ClassSubjectTeacherId is null)
                return OperationResult<TimetableSlotDto>.Failure(
                    "يجب تحديد تعيين المعلم عند إضافة حصة عادية");

            var cst = await _unitOfWork.ClassSubjectTeachers
                .GetByIdAsync(request.ClassSubjectTeacherId.Value);
            if (cst is null || cst.IsDeleted)
                return OperationResult<TimetableSlotDto>.Failure(
                    "تعيين المعلم غير موجود");

            if (cst.ClassId != timetable.ClassId)
                return OperationResult<TimetableSlotDto>.Failure(
                    "تعيين المعلم تابع لفصل مختلف");

            if (await _unitOfWork.TimetableSlots.HasTeacherConflictAsync(
                    cst.TeacherId,
                    cst.AcademicYearId,
                    request.DayOfWeek,
                    request.PeriodNumber))
                return OperationResult<TimetableSlotDto>.Failure(
                    "المعلم لديه حصة أخرى في نفس اليوم ونفس الرقم");
        }

        // 5. Validate room (optional)
        if (request.RoomId.HasValue)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(request.RoomId.Value);
            if (room is null || room.IsDeleted)
                return OperationResult<TimetableSlotDto>.Failure("الغرفة غير موجودة");
        }

        // 6. Create slot
        var slot = new TimetableSlot
        {
            TimetableId           = request.TimetableId,
            DayOfWeek             = request.DayOfWeek,
            PeriodNumber          = request.PeriodNumber,
            StartTime             = request.StartTime,
            EndTime               = request.EndTime,
            IsBreak               = request.IsBreak,
            ClassSubjectTeacherId = request.ClassSubjectTeacherId,
            RoomId                = request.RoomId
        };

        // 7. Persist
        await _unitOfWork.TimetableSlots.AddAsync(slot);
        await _unitOfWork.SaveChangesAsync();

        // 7. Reload with ClassSubjectTeacher, Subject, and Teacher for DTO names
        var withDetails = await _unitOfWork.TimetableSlots.GetByIdWithDetailsAsync(slot.Id);

        return OperationResult<TimetableSlotDto>.Success(
            _mapper.Map<TimetableSlotDto>(withDetails),
            "تمت إضافة الحصة بنجاح");
    }

    public async Task<OperationResult<TimetableSlotDto>> UpdateTimetableSlotAsync(
        UpdateTimetableSlotRequest request)
    {
        // 1. Find slot
        var slot = await _unitOfWork.TimetableSlots.GetByIdAsync(request.SlotId);
        if (slot is null || slot.IsDeleted)
            return OperationResult<TimetableSlotDto>.Failure("الحصة غير موجودة");

        var timetable = await _unitOfWork.Timetables.GetByIdAsync(slot.TimetableId);
        if (timetable is null || timetable.IsDeleted)
            return OperationResult<TimetableSlotDto>.Failure("الجدول الدراسي غير موجود");

        // 2. Day/period uniqueness excluding the current slot
        if (await _unitOfWork.TimetableSlots.HasConflictAsync(
                slot.TimetableId,
                request.DayOfWeek,
                request.PeriodNumber,
                slot.Id))
            return OperationResult<TimetableSlotDto>.Failure(
                "توجد حصة بالفعل في هذا اليوم وهذا الرقم");

        // 3. Time range validation
        if (request.EndTime <= request.StartTime)
            return OperationResult<TimetableSlotDto>.Failure(
                "وقت النهاية يجب أن يكون بعد وقت البداية");

        // 4. Validate non-break assignment
        if (!request.IsBreak)
        {
            if (request.ClassSubjectTeacherId is null)
                return OperationResult<TimetableSlotDto>.Failure(
                    "يجب تحديد تعيين المعلم عند تعديل حصة عادية");

            var cst = await _unitOfWork.ClassSubjectTeachers
                .GetByIdAsync(request.ClassSubjectTeacherId.Value);
            if (cst is null || cst.IsDeleted)
                return OperationResult<TimetableSlotDto>.Failure(
                    "تعيين المعلم غير موجود");

            if (cst.ClassId != timetable.ClassId)
                return OperationResult<TimetableSlotDto>.Failure(
                    "تعيين المعلم تابع لفصل مختلف");

            if (await _unitOfWork.TimetableSlots.HasTeacherConflictAsync(
                    cst.TeacherId,
                    cst.AcademicYearId,
                    request.DayOfWeek,
                    request.PeriodNumber,
                    slot.Id))
                return OperationResult<TimetableSlotDto>.Failure(
                    "المعلم لديه حصة أخرى في نفس اليوم ونفس الرقم");
        }

        // 5. Validate room (optional)
        if (request.RoomId.HasValue)
        {
            var room = await _unitOfWork.Rooms.GetByIdAsync(request.RoomId.Value);
            if (room is null || room.IsDeleted)
                return OperationResult<TimetableSlotDto>.Failure("الغرفة غير موجودة");
        }

        // 6. Apply updates
        slot.DayOfWeek             = request.DayOfWeek;
        slot.PeriodNumber          = request.PeriodNumber;
        slot.StartTime             = request.StartTime;
        slot.EndTime               = request.EndTime;
        slot.IsBreak               = request.IsBreak;
        slot.ClassSubjectTeacherId = request.IsBreak ? null : request.ClassSubjectTeacherId;
        slot.RoomId                = request.RoomId;
        slot.UpdatedAt             = DateTime.UtcNow;

        _unitOfWork.TimetableSlots.Update(slot);
        await _unitOfWork.SaveChangesAsync();

        // 7. Reload with details for SubjectName, TeacherName, and RoomName
        var withDetails = await _unitOfWork.TimetableSlots.GetByIdWithDetailsAsync(slot.Id);

        return OperationResult<TimetableSlotDto>.Success(
            _mapper.Map<TimetableSlotDto>(withDetails),
            "تم تحديث الحصة بنجاح");
    }

    public async Task<OperationResult> DeleteTimetableSlotAsync(int slotId)
    {
        var slot = await _unitOfWork.TimetableSlots.GetByIdAsync(slotId);
        if (slot is null || slot.IsDeleted)
            return OperationResult.Failure("الحصة غير موجودة");

        _unitOfWork.TimetableSlots.SoftDelete(slot);
        await _unitOfWork.SaveChangesAsync();

        return OperationResult.Success("تم حذف الحصة بنجاح");
    }

    public async Task<OperationResult<IEnumerable<TimetableDto>>> GetTimetablesByClassAndYearAsync(
        int classId, int academicYearId)
    {
        var schoolClass = await _unitOfWork.Classes.GetByIdAsync(classId);
        if (schoolClass is null || schoolClass.IsDeleted)
            return OperationResult<IEnumerable<TimetableDto>>.Failure("الفصل غير موجود");

        var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId);
        if (academicYear is null || academicYear.IsDeleted)
            return OperationResult<IEnumerable<TimetableDto>>.Failure("السنة الدراسية غير موجودة");

        var timetables = await _unitOfWork.Timetables
            .GetByClassAndYearWithDetailsAsync(classId, academicYearId);

        return OperationResult<IEnumerable<TimetableDto>>.Success(
            _mapper.Map<IEnumerable<TimetableDto>>(timetables),
            "تم جلب الجداول الدراسية بنجاح");
    }

    public async Task<OperationResult<TimetableDto>> GetByClassAsync(
        int classId, int academicYearId)
    {
        var timetable = await _unitOfWork.Timetables
            .GetActiveByClassAndYearAsync(classId, academicYearId);
        if (timetable is null)
            return OperationResult<TimetableDto>.Failure(
                "لا يوجد جدول دراسي مفعل لهذا الفصل وهذه السنة الدراسية");

        var full = await _unitOfWork.Timetables
            .GetWithClassAndAllSlotsAsync(timetable.Id);

        return OperationResult<TimetableDto>.Success(
            _mapper.Map<TimetableDto>(full),
            "تم جلب الجدول الدراسي بنجاح");
    }

    public async Task<OperationResult<TimetableDto>> GetByStudentAsync(int enrollmentId)
    {
        // 1. Find enrollment
        var enrollment = await _unitOfWork.StudentEnrollments.GetByIdAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<TimetableDto>.Failure("تسجيل الطالب غير موجود");

        // 2. Resolve class and year using the same active timetable lookup
        var timetable = await _unitOfWork.Timetables.GetActiveByClassAndYearAsync(
            enrollment.ClassId, enrollment.AcademicYearId);
        if (timetable is null)
            return OperationResult<TimetableDto>.Failure(
                "لا يوجد جدول دراسي مفعل لهذا الفصل وهذه السنة الدراسية");

        var full = await _unitOfWork.Timetables
            .GetWithClassAndAllSlotsAsync(timetable.Id);

        return OperationResult<TimetableDto>.Success(
            _mapper.Map<TimetableDto>(full),
            "تم جلب جدول الطالب بنجاح");
    }

    public async Task<OperationResult<TimetableDto>> GetByStudentForUserAsync(int enrollmentId, int userId)
    {
        var enrollment = await _unitOfWork.StudentEnrollments.GetWithStudentAsync(enrollmentId);
        if (enrollment is null || enrollment.IsDeleted)
            return OperationResult<TimetableDto>.Failure("تسجيل الطالب غير موجود");

        var student = enrollment.Student;
        if (student.UserId != userId)
        {
            var linked = await _unitOfWork.ParentStudents.GetByParentAndStudentAsync(userId, student.Id);
            if (linked is null)
                return OperationResult<TimetableDto>.Failure("غير مسموح بالوصول لهذا الجدول");
        }

        var timetable = await _unitOfWork.Timetables.GetActiveByClassAndYearAsync(
            enrollment.ClassId, enrollment.AcademicYearId);
        if (timetable is null)
            return OperationResult<TimetableDto>.Failure(
                "لا يوجد جدول دراسي مفعل لهذا الفصل وهذه السنة الدراسية");

        var full = await _unitOfWork.Timetables
            .GetWithClassAndAllSlotsAsync(timetable.Id);

        return OperationResult<TimetableDto>.Success(
            _mapper.Map<TimetableDto>(full),
            "تم جلب جدول الطالب بنجاح");
    }

    public async Task<OperationResult<TimetableDto>> GetMyStudentScheduleAsync(int studentUserId, int academicYearId)
    {
        var student = await _unitOfWork.Students.GetByUserIdAsync(studentUserId);
        if (student is null || student.IsDeleted)
            return OperationResult<TimetableDto>.Failure("الطالب غير موجود");

        var enrollment = await _unitOfWork.StudentEnrollments
            .GetActiveByStudentAndYearAsync(student.Id, academicYearId);
        if (enrollment is null)
            return OperationResult<TimetableDto>.Failure("لا يوجد تسجيل نشط لهذا الطالب في هذه السنة الدراسية");

        return await GetByStudentAsync(enrollment.Id);
    }

    public async Task<OperationResult<IEnumerable<ChildScheduleDto>>> GetMyChildSchedulesAsync(int parentUserId, int academicYearId)
    {
        var links = await _unitOfWork.ParentStudents.GetWithStudentDetailsByParentAsync(parentUserId);
        var schedules = new List<ChildScheduleDto>();

        foreach (var link in links.Where(l => !l.IsDeleted && l.Student is not null))
        {
            var student   = link.Student;
            var studentId = link.StudentId;

            var enrollment = await _unitOfWork.StudentEnrollments
                .GetActiveByStudentAndYearAsync(studentId, academicYearId);
            if (enrollment is null)
                continue;

            var timetable = await _unitOfWork.Timetables.GetActiveByClassAndYearAsync(
                enrollment.ClassId, enrollment.AcademicYearId);
            if (timetable is null)
                continue;

            var full = await _unitOfWork.Timetables.GetWithClassAndAllSlotsAsync(timetable.Id);
            var dto  = _mapper.Map<ChildScheduleDto>(full);

            // FIX: نحط اسم الطالب في الـ DTO عشان ولي الأمر يعرف جدول مين ده
            dto.StudentId   = student.Id;
            dto.StudentName = student.FullName;

            schedules.Add(dto);
        }

        return OperationResult<IEnumerable<ChildScheduleDto>>.Success(
            schedules,
            "تم جلب جداول الأبناء بنجاح");
    }

    public async Task<OperationResult<IEnumerable<TeacherScheduleSlotDto>>> GetTeacherScheduleAsync(
        int teacherId, int academicYearId)
    {
        var teacher = await _unitOfWork.Users.GetByIdAsync(teacherId);
        if (teacher is null || teacher.IsDeleted)
            return OperationResult<IEnumerable<TeacherScheduleSlotDto>>.Failure("المعلم غير موجود");
        if (teacher.Role != UserRole.Teacher)
            return OperationResult<IEnumerable<TeacherScheduleSlotDto>>.Failure("المستخدم ليس معلما");

        var academicYear = await _unitOfWork.AcademicYears.GetByIdAsync(academicYearId);
        if (academicYear is null || academicYear.IsDeleted)
            return OperationResult<IEnumerable<TeacherScheduleSlotDto>>.Failure("السنة الدراسية غير موجودة");

        var slots = await _unitOfWork.TimetableSlots.GetTeacherScheduleAsync(teacherId, academicYearId);
        var result = slots.Select(slot => new TeacherScheduleSlotDto
        {
            Id                    = slot.Id,
            TimetableId           = slot.TimetableId,
            ClassId               = slot.Timetable.ClassId,
            ClassName             = slot.Timetable.Class.Name,
            DayOfWeek             = slot.DayOfWeek.ToString(),
            PeriodNumber          = slot.PeriodNumber,
            StartTime             = slot.StartTime,
            EndTime               = slot.EndTime,
            // FIX 2: IsBreak كانت مش بتتحط في الـ DTO خالص
            IsBreak               = slot.IsBreak,
            ClassSubjectTeacherId = slot.ClassSubjectTeacherId,
            SubjectName           = slot.ClassSubjectTeacher?.Subject.Name,
            RoomName              = slot.Room?.Name
        });

        return OperationResult<IEnumerable<TeacherScheduleSlotDto>>.Success(
            result,
            "تم جلب جدول المعلم بنجاح");
    }
}
