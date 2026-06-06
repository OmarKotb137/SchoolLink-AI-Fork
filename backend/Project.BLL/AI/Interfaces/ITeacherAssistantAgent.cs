using Project.BLL.AI.Models;
using Common.Results;

namespace Project.BLL.AI.Interfaces;

public interface ITeacherAssistantAgent
{
    Task<OperationResult<AgentResponse>> SuggestLessonPlanAsync(LessonPlanRequest request, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> GenerateQuizAsync(string subject, string topic, int count = 10, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> GradeSubmissionAsync(string questionText, string modelAnswer, string studentAnswer, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> SuggestTeachingResourcesAsync(string subject, string topic, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> AnalyzeClassPerformanceAsync(List<Dictionary<string, object>> studentScores, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> ChatAsync(string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default);
}
