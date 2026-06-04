using Common.Results;
using Project.BLL.DTOs.Students;

namespace Project.BLL.Interfaces;

public interface IStudentService
{
    Task<OperationResult<StudentDto>> CreateStudentAsync(CreateStudentRequest request);
    Task<OperationResult<StudentDto>> UpdateStudentAsync(UpdateStudentRequest request);
    Task<OperationResult> LinkUserAccountAsync(LinkStudentUserRequest request);
    Task<OperationResult<StudentDto>> GetStudentByIdAsync(int id);
    Task<OperationResult<IEnumerable<StudentDto>>> GetAllStudentsAsync();
    Task<OperationResult<StudentDto>> GetStudentByUserIdAsync(int userId);
    Task<OperationResult<IEnumerable<StudentDto>>> SearchStudentsAsync(StudentSearchFilter filter);
    Task<OperationResult> DeleteStudentAsync(int id);
    Task<OperationResult<BulkCreateStudentsResultDto>> BulkCreateStudentsAsync(BulkCreateStudentsRequest request);
}
