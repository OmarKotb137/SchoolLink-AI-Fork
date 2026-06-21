using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface ITimetableService
{
    Task<OperationResult<TimetableDto>>     CreateTimetableAsync(CreateTimetableRequest request, CancellationToken ct = default);
    Task<OperationResult<TimetableDto>>     CloneDraftTimetableAsync(int classId, int academicYearId, bool replaceExisting = false, CancellationToken ct = default);
    Task<OperationResult<TimetableDto>>     GetTimetableByIdAsync(int timetableId, CancellationToken ct = default);
    Task<OperationResult<TimetableValidationResultDto>> ValidateTimetableAsync(int timetableId, CancellationToken ct = default);
    Task<OperationResult>                   ActivateTimetableAsync(int timetableId, CancellationToken ct = default);
    Task<OperationResult>                   DeactivateTimetableAsync(int timetableId, CancellationToken ct = default);
    Task<OperationResult>                   DeleteTimetableAsync(int timetableId, CancellationToken ct = default);
    Task<OperationResult<TimetableSlotDto>> AddTimetableSlotAsync(AddTimetableSlotRequest request, CancellationToken ct = default);
    Task<OperationResult<TimetableSlotDto>> UpdateTimetableSlotAsync(UpdateTimetableSlotRequest request, CancellationToken ct = default);
    Task<OperationResult>                   DeleteTimetableSlotAsync(int slotId, CancellationToken ct = default);

    /// <summary>
    /// يحدّث توقيت (StartTime/EndTime) كل الحصص برقم حصة معيّن داخل الجدول دفعة واحدة.
    /// يُستخدم من رأس الجدول الموحّد لتطبيق التوقيت على عمود كامل (كل الأيام).
    /// </summary>
    Task<OperationResult> UpdatePeriodTimingAsync(
        int timetableId,
        UpdatePeriodTimingRequest request,
        CancellationToken ct = default);
    Task<OperationResult<IEnumerable<TimetableDto>>> GetTimetablesByClassAndYearAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<OperationResult<TimetableDto>>     GetByClassAsync(int classId, int academicYearId, CancellationToken ct = default);
    Task<OperationResult<TimetableDto>>     GetByStudentAsync(int enrollmentId, CancellationToken ct = default);
    Task<OperationResult<IEnumerable<TeacherScheduleSlotDto>>> GetTeacherScheduleAsync(int teacherId, int academicYearId, CancellationToken ct = default);
    Task<OperationResult<TimetableDto>>     GetByStudentForUserAsync(int enrollmentId, int userId, CancellationToken ct = default);
    Task<OperationResult<IEnumerable<ChildScheduleDto>>> GetMyChildSchedulesAsync(int parentUserId, int academicYearId, CancellationToken ct = default);
    Task<OperationResult<TimetableDto>>     GetMyStudentScheduleAsync(int studentUserId, int academicYearId, CancellationToken ct = default);
}
