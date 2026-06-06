using Common.Results;
using Project.BLL.DTOs.Exam;

namespace Project.BLL.AI.Interfaces;

public interface IExamGeneratorService
{
    Task<OperationResult<GetExamDto>> GenerateExamAsync(CreateExamFromAiDto dto, CancellationToken ct = default);
    Task<OperationResult<GetExamDto>> RegenerateQuestionsAsync(int examId, List<int> questionIds, CancellationToken ct = default);
}
