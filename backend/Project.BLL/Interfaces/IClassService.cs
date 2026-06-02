using Common.Results;
using Project.BLL.DTOs;

namespace Project.BLL.Interfaces;

public interface IClassService
{
    Task<OperationResult<ClassDto>>              CreateClassAsync(CreateClassRequest request);
    Task<OperationResult<ClassDto>>              UpdateClassAsync(UpdateClassRequest request);
    Task<OperationResult>                        DeleteClassAsync(int id);
    Task<OperationResult<IEnumerable<ClassDto>>> GetAllClassesAsync(GetClassesFilter filter);
    Task<OperationResult<ClassDto>>              GetClassByIdAsync(int id);
}
