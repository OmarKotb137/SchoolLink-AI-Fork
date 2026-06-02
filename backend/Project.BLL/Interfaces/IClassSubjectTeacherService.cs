using Common.Results;
using Project.BLL.DTOs;
using Project.BLL.DTOs.Users;

namespace Project.BLL.Interfaces;

public interface IClassSubjectTeacherService
{
    Task<OperationResult<ClassSubjectTeacherDto>>              AssignTeacherAsync(AssignTeacherRequest request);
    Task<OperationResult<ClassSubjectTeacherDto>>              GetAssignmentByIdAsync(int id);
    Task<OperationResult<ClassSubjectTeacherDto>>              UpdateTeacherAssignmentAsync(UpdateTeacherAssignmentRequest request);
    Task<OperationResult>                                      UnassignTeacherAsync(int id);
    Task<OperationResult<IEnumerable<ClassSubjectTeacherDto>>> GetByClassAsync(int classId, int academicYearId);
    Task<OperationResult<IEnumerable<ClassSubjectTeacherDto>>> GetByTeacherAsync(int teacherId, int academicYearId);
    Task<OperationResult<IEnumerable<UserDto>>>                GetAvailableTeachersForSubjectAsync(int subjectId, int classId, int academicYearId);
    Task<OperationResult>                                      BulkAssignTeachersAsync(List<AssignTeacherRequest> requests);
}
