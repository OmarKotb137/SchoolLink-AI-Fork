using Project.BLL.AI.Models;
using Common.Results;

namespace Project.BLL.AI.Interfaces;

public interface IParentAssistantAgent
{
    Task<OperationResult<AgentResponse>> GetProgressSummaryAsync(int studentId, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> SuggestLearningActivitiesAsync(int studentId, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> IdentifyWeakAreasAsync(int studentId, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> RecommendResourcesAsync(string subject, string gradeLevel, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> ChatAsync(string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default);
}
