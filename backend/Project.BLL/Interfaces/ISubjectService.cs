using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface ISubjectService
{
    Task<OperationResult<SubjectDto>>              CreateSubjectAsync(CreateSubjectRequest request);
    Task<OperationResult<SubjectDto>>              UpdateSubjectAsync(UpdateSubjectRequest request);
    Task<OperationResult>                          DeleteSubjectAsync(int id);
    Task<OperationResult<SubjectDto>>              GetSubjectByIdAsync(int id);
    Task<OperationResult<IEnumerable<SubjectDto>>> GetAllSubjectsAsync();
    Task<OperationResult<IEnumerable<SubjectDto>>> GetSubjectsByGradeLevelAsync(int gradeLevelId);
    Task<OperationResult<IEnumerable<SubjectDto>>> GetSubjectsByTeacherAsync(int teacherId, int academicYearId);
    Task<OperationResult<IEnumerable<SubjectDto>>> SearchSubjectsAsync(string term);

    /// <summary>
    /// Returns the actual ClassSubjectTeacher assignments for a teacher.
    /// Each row = one specific subject + class combo with its own Id.
    /// Used by AI tools so the LLM picks the correct classSubjectTeacherId.
    /// </summary>
    Task<OperationResult<IEnumerable<TeacherSubjectAssignmentDto>>> GetAssignmentsByTeacherAsync(int teacherId, int academicYearId);
}
