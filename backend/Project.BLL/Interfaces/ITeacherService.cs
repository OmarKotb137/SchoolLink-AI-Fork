using Common.Results;
using Project.BLL.DTOs.Common;
using Project.BLL.DTOs.Teachers;

namespace Project.BLL.Interfaces;

public interface ITeacherService
{
    Task<OperationResult<TeacherDto>> CreateTeacherAsync(CreateTeacherRequest request);
    Task<OperationResult<TeacherDto>> UpdateTeacherAsync(UpdateTeacherRequest request);
    Task<OperationResult<TeacherDto>> GetTeacherByIdAsync(int id);
    Task<OperationResult<PagedResult<TeacherDto>>> GetAllTeachersAsync(PaginationFilter filter);
    Task<OperationResult> DeleteTeacherAsync(int id);
}
