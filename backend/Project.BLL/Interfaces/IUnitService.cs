using Common.Results;
using Project.BLL.DTOs;
using Project.Domain.Enums;

namespace Project.BLL.Interfaces;

public interface IUnitService
{
    Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, string name, int displayOrder, List<CreateLessonDto>? lessons = null, AcademicTerm? term = null);
    Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, CreateUnitDto dto);
    Task<OperationResult<List<UnitDto>>> GetUnitsBySubjectAsync(int subjectId, AcademicTerm? term = null);
    Task<OperationResult<List<UnitDto>>> GetUnitsByGradeLevelAndSubjectAsync(int gradeLevelId, int subjectId, AcademicTerm? term = null);
    Task<OperationResult<List<LessonDto>>> GetLessonsByUnitAsync(int unitId);
    Task<OperationResult<List<SubjectWithStructureDto>>> GetParsedSubjectsWithStructureAsync(AcademicTerm? term = null);
    Task<OperationResult<List<UnitDto>>> GetUnitsWithLessonsBySubjectAsync(int subjectId, AcademicTerm? term = null);
    Task<OperationResult<UnitDto>> UpdateUnitAsync(int id, string name, string? content = null, int? pageStart = null, int? pageEnd = null, AcademicTerm? term = null);
    Task<OperationResult<LessonDto>> UpdateLessonAsync(int id, string title, string? content = null, int? pageStart = null, int? pageEnd = null);
    Task<OperationResult<LessonDto>> CreateLessonAsync(int unitId, CreateLessonDto dto);
    Task<OperationResult> DeleteUnitAsync(int unitId);
    Task<OperationResult> DeleteLessonAsync(int lessonId);
}
