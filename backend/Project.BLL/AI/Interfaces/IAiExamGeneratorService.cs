using Common.Results;
using Project.BLL.DTOs.Exam;

namespace Project.BLL.AI.Interfaces;

public interface IAiExamGeneratorService
{
    Task<OperationResult<GetExamDto>> GenerateExamAsync(AiGenerateExamRequest request, CancellationToken ct = default);
    Task<OperationResult<AiExamPreviewDto>> PreviewExamAsync(AiGenerateExamRequest request, CancellationToken ct = default);
    Task<OperationResult<GetExamDto>> SaveGeneratedExamAsync(CreateExamFromAiDto dto, CancellationToken ct = default);
}
