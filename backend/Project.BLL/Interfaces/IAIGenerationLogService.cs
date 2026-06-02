using Common.Results;
using Project.BLL.DTOs.AIGenerationLog;

namespace Project.BLL.Interfaces
{
    public interface IAIGenerationLogService
    {
        Task<OperationResult<List<GetAIGenerationLogDto>>> GetAllAsync();
        Task<OperationResult<List<GetAIGenerationLogDto>>> GetByUserIdAsync(int userId);
        Task<OperationResult<GetAIGenerationLogDto>> GetByIdAsync(int id);
        Task<OperationResult<GetAIGenerationLogDto>> CreateAsync(CreateAIGenerationLogDto dto);
        Task<OperationResult<AIGenerationLogSummaryDto>> GetSummaryAsync();
    }
}