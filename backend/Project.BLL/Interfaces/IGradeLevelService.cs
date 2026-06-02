using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface IGradeLevelService
{
    Task<OperationResult<GradeLevelDto>>              CreateGradeLevelAsync(CreateGradeLevelRequest request);
    Task<OperationResult<GradeLevelDto>>              UpdateGradeLevelAsync(UpdateGradeLevelRequest request);
    Task<OperationResult>                             DeleteGradeLevelAsync(int id);
    Task<OperationResult<GradeLevelDto>>              GetGradeLevelByIdAsync(int id);
    Task<OperationResult<IEnumerable<GradeLevelDto>>> GetAllGradeLevelsAsync();
    Task<OperationResult<GradeLevelDto>>             GetGradeLevelWithClassesAsync(int id);
}
