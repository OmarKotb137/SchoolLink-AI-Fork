using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface IUnitService
{
    Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, string name, int displayOrder, List<CreateLessonDto>? lessons = null);
    Task<OperationResult<UnitDto>> CreateUnitAsync(int subjectId, CreateUnitDto dto);
    Task<OperationResult<List<UnitDto>>> GetUnitsBySubjectAsync(int subjectId);
    Task<OperationResult<List<LessonDto>>> GetLessonsByUnitAsync(int unitId);
    Task<OperationResult> DeleteUnitAsync(int unitId);
}
