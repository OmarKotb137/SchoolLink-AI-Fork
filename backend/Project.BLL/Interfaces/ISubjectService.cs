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
}
