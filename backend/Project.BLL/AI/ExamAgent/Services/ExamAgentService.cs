using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.ExamAgent.Services;

public class ExamAgentService
{
    private readonly ILlmClient _llm;
    private readonly AgentToolRegistry _registry;
    private readonly ILogger<ExamAgentService> _logger;
    private readonly IAgentChatStore _chatStore;

    public ExamAgentService(
        ILlmClient llm,
        AgentToolRegistry registry,
        ILogger<ExamAgentService> logger,
        IAgentChatStore chatStore)
    {
        _llm = llm;
        _registry = registry;
        _logger = logger;
        _chatStore = chatStore;
    }

    public async Task<AgentChatResponse> RunAsync(string userRequest, string? conversationId = null)
    {
        conversationId ??= Guid.NewGuid().ToString();

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System,
                """
                أنت Agent ذكي متخصص في بناء الامتحانات التعليمية.
                لديك أدوات (tools) يمكنك استخدامها.

                القواعد الإجبارية:
                1. قبل توليد أي امتحان، يجب أن تجلب محتوى الدرس أولاً باستخدام get_lesson_content.
                2. لا تولّد امتحاناً من الذاكرة أو التخمين — استخدم المحتوى الحقيقي فقط.
                3. إذا لم يحدد المستخدم الدرس، استخدم get_lessons أولاً لعرض القائمة عليه.
                """)
        };

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, 10, CancellationToken.None);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(msg.Role == "user" ? MessageRole.User : MessageRole.Assistant, msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, userRequest));
        await _chatStore.SaveMessageAsync(conversationId, "user", userRequest, "exam", CancellationToken.None);

        var toolDefs = _registry.All.Select(t => t.ToFunctionDefinition()).ToList();

        for (int step = 0; step < 10; step++)
        {
            _logger.LogInformation("Agent step {Step} for conversation {ConvId}", step + 1, conversationId);

            var response = await _llm.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = response.Content ?? "لم يتمكن الـ Agent من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, "assistant", answer, "exam", CancellationToken.None);
                return new AgentChatResponse { Answer = answer };
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                response.Content ?? "",
                toolCalls: response.ToolCalls));

            foreach (var call in response.ToolCalls)
            {
                _logger.LogInformation("Calling tool: {Tool} with args: {Args}", call.Name, call.Arguments);

                var tool = _registry.Get(call.Name);
                if (tool is null)
                {
                    messages.Add(new LlmChatMessage(
                        MessageRole.Tool,
                        $"{{\"error\": \"Tool '{call.Name}' not found\"}}",
                        toolCallId: call.Id));
                    continue;
                }

                JsonElement argsElement;
                try
                {
                    argsElement = JsonDocument.Parse(call.Arguments).RootElement;
                }
                catch
                {
                    argsElement = JsonDocument.Parse("{}").RootElement;
                }

                var result = await tool.ExecuteAsync(argsElement);

                _logger.LogInformation("Tool {Tool} result: {Success}", call.Name, result.Success);

                messages.Add(new LlmChatMessage(
                    MessageRole.Tool,
                    result.ToJson(),
                    toolCallId: call.Id));
            }
        }

        _logger.LogError("Agent exceeded maximum steps (10) for conversation {ConvId}", conversationId);
        throw new InvalidOperationException("Agent exceeded maximum steps (10). Check the LLM behavior.");
    }
}
