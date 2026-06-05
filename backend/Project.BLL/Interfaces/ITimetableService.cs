using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface ITimetableService
{
    Task<OperationResult<TimetableDto>>     CreateTimetableAsync(CreateTimetableRequest request);
    Task<OperationResult<TimetableDto>>     GetTimetableByIdAsync(int timetableId);
    Task<OperationResult>                   ActivateTimetableAsync(int timetableId);
    Task<OperationResult>                   DeactivateTimetableAsync(int timetableId);
    Task<OperationResult>                   DeleteTimetableAsync(int timetableId);
    Task<OperationResult<TimetableSlotDto>> AddTimetableSlotAsync(AddTimetableSlotRequest request);
    Task<OperationResult<TimetableSlotDto>> UpdateTimetableSlotAsync(UpdateTimetableSlotRequest request);
    Task<OperationResult>                   DeleteTimetableSlotAsync(int slotId);
    Task<OperationResult<IEnumerable<TimetableDto>>> GetTimetablesByClassAndYearAsync(int classId, int academicYearId);
    Task<OperationResult<TimetableDto>>     GetByClassAsync(int classId, int academicYearId);
    Task<OperationResult<TimetableDto>>     GetByStudentAsync(int enrollmentId);
    Task<OperationResult<IEnumerable<TeacherScheduleSlotDto>>> GetTeacherScheduleAsync(int teacherId, int academicYearId);
    Task<OperationResult<TimetableDto>>     GetByStudentForUserAsync(int enrollmentId, int userId);
    Task<OperationResult<IEnumerable<ChildScheduleDto>>> GetMyChildSchedulesAsync(int parentUserId, int academicYearId);
    Task<OperationResult<TimetableDto>>     GetMyStudentScheduleAsync(int studentUserId, int academicYearId);
}
