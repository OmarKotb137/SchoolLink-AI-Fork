using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface IUnitService
{
    Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, string name, int displayOrder, List<CreateLessonDto>? lessons = null);
    Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, CreateUnitDto dto);
    Task<OperationResult<List<UnitDto>>> GetUnitsBySubjectAsync(int subjectId);
    Task<OperationResult<List<LessonDto>>> GetLessonsByUnitAsync(int unitId);
    Task<OperationResult<List<SubjectWithStructureDto>>> GetParsedSubjectsWithStructureAsync();
    Task<OperationResult<List<UnitDto>>> GetUnitsWithLessonsBySubjectAsync(int subjectId);
    Task<OperationResult<UnitDto>> UpdateUnitAsync(int id, string name, string? content = null, int? pageStart = null, int? pageEnd = null);
    Task<OperationResult<LessonDto>> UpdateLessonAsync(int id, string title, string? content = null, int? pageStart = null, int? pageEnd = null);
    Task<OperationResult<LessonDto>> CreateLessonAsync(int unitId, CreateLessonDto dto);
    Task<OperationResult> DeleteUnitAsync(int unitId);
    Task<OperationResult> DeleteLessonAsync(int lessonId);
}
