using Common.Results;
using Project.BLL.DTOs.QuestionBank;

namespace Project.BLL.Interfaces;

public interface IQuestionBankService
{
    Task<OperationResult<List<QuestionBankItemDto>>> GetBySubjectAsync(int subjectId, int? gradeLevelId = null);
    Task<OperationResult<QuestionBankItemDto>> GetByIdAsync(int id);
    Task<OperationResult<PagedResultDto<QuestionBankItemDto>>> SearchAsync(SearchQuestionBankDto dto);
    Task<OperationResult<QuestionBankItemDto>> AddQuestionAsync(AddToQuestionBankDto dto);
    Task<OperationResult<int>> BulkAddFromExamAsync(int examId, int subjectId);
    Task<OperationResult> DeleteAsync(int id);
    Task<OperationResult<QuestionBankItemDto>> UpdateAsync(int id, AddToQuestionBankDto dto);
}
