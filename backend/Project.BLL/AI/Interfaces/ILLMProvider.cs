using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface ILLMProvider
{
    string ProviderName { get; }
    Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
    Task<string> GenerateChatAsync(string systemPrompt, List<ChatMessage> messages, CancellationToken ct = default);
    Task<string> GenerateWithToolsAsync(string systemPrompt, string userMessage, List<AiTool> tools, CancellationToken ct = default);
}
