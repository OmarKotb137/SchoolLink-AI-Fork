using Project.BLL.AI.Models;

namespace Project.BLL.AI.Interfaces;

public interface ILlmClient
{
    Task<LlmResponse> ChatAsync(
        List<LlmChatMessage> messages,
        IEnumerable<FunctionDefinition> tools);
}
