using Common.Results;
using Project.BLL.DTOs.AccountGeneration;

namespace Project.BLL.Interfaces;

public interface IAccountGenerationService
{
    Task<OperationResult<IEnumerable<StudentAccountCandidateDto>>> GetStudentAccountCandidatesAsync();
    Task<OperationResult<GenerateStudentAccountResultDto>> GenerateStudentAccountAsync(int studentId);
    Task<OperationResult<GenerateBulkStudentAccountsResultDto>> GenerateBulkStudentAccountsAsync(List<int> studentIds);
    Task<OperationResult<CreateParentWithStudentsResultDto>> CreateParentWithStudentsAsync(CreateParentWithStudentsRequest request);
    Task<OperationResult<ParentPhoneCheckDto>> CheckParentPhoneAsync(string phone);
}
