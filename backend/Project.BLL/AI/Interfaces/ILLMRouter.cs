using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface ILLMRouter
{
    Task<string> GenerateAsync(string systemPrompt, string userMessage, string? preferredProvider = null, CancellationToken ct = default);
    Task<string> GenerateChatAsync(string systemPrompt, List<ChatMessage> messages, string? preferredProvider = null, CancellationToken ct = default);
    Task<string> GenerateWithToolsAsync(string systemPrompt, string userMessage, List<AiTool> tools, string? preferredProvider = null, CancellationToken ct = default);
}
