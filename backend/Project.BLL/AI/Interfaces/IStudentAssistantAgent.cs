using Project.BLL.AI.Models;
using Common.Results;
using Project.Domain.Enums;

namespace Project.BLL.AI.Interfaces;

public interface IStudentAssistantAgent
{
    Task<OperationResult<AgentResponse>> AnswerQuestionAsync(AiQuestionRequest request, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> ExplainConceptAsync(string concept, string subject, string gradeLevel, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> GeneratePracticeExerciseAsync(string subject, string topic, int count = 5, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> AnalyzeAnswerAsync(string questionText, string studentAnswer, string? modelAnswer = null, AcademicTerm? term = null, CancellationToken ct = default);
    Task<OperationResult<AgentResponse>> ChatAsync(string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default);
}
