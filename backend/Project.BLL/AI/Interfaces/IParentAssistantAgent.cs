using Project.BLL.AI.Models;
using Common.Results;
using Project.Domain.Enums;

namespace Project.BLL.AI.Interfaces;

public interface IParentAssistantAgent
{
    Task<OperationResult<AgentResponse>> GetProgressSummaryAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> SuggestLearningActivitiesAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> IdentifyWeakAreasAsync(int studentId, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> RecommendResourcesAsync(string subject, string gradeLevel, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> ChatAsync(string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default);
}
