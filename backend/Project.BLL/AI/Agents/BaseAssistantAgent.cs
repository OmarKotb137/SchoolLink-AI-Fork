using System.Text.Json;
using System.Text.RegularExpressions;
using Common.Results;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.DAL.Interfaces;

namespace Project.BLL.AI.Agents;

public abstract class BaseAssistantAgent
{
    protected readonly ILLMRouter _router;
    protected readonly ILlmClient _llmClient;
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IAgentChatStore _chatStore;
    protected readonly ILogger _logger;

    protected BaseAssistantAgent(
        ILLMRouter router,
        ILlmClient llmClient,
        IUnitOfWork unitOfWork,
        ILogger logger,
        IAgentChatStore chatStore)
    {
        _router = router;
        _llmClient = llmClient;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _chatStore = chatStore;
    }

    protected abstract string SystemPrompt { get; }
    protected abstract string AgentType { get; }
    protected abstract Dictionary<string, AiTool> CreateTools(UserContext context, CancellationToken ct);
    protected abstract Task ResolveContextAsync(UserContext context, CancellationToken ct);
    protected abstract List<string> GetDynamicSuggestions(string lastToolCalled);
    protected virtual string GetContextHint(UserContext context) => "";

    public async Task<OperationResult<AgentResponse>> ChatAsync(
        string message, string? conversationId = null, UserContext? context = null, CancellationToken ct = default)
    {
        conversationId ??= Guid.NewGuid().ToString();
        context ??= new UserContext();

        await ResolveContextAsync(context, ct);

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, SystemPrompt + GetContextHint(context))
        };

        var history = await _chatStore.GetRecentMessagesAsync(conversationId, context.UserId, 50, CancellationToken.None);
        foreach (var msg in history)
            messages.Add(new LlmChatMessage(msg.ToMessageRole(), msg.Content));

        messages.Add(new LlmChatMessage(MessageRole.User, message));
        await _chatStore.SaveMessageAsync(conversationId, context.UserId, "user", message, AgentType, CancellationToken.None);

        var tools = CreateTools(context, ct);
        var toolDefs = tools.Values.Select(t => new FunctionDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.Parameters ?? JsonDocument.Parse("{}").RootElement
        }).ToList();

        var lastToolCalled = "";

        for (int step = 0; step < 10; step++)
        {
            _logger.LogInformation("{AgentType} step {Step} for conv {ConvId}", AgentType, step + 1, conversationId);

            var response = await _llmClient.ChatAsync(messages, toolDefs);

            if (response.ToolCalls is null || response.ToolCalls.Count == 0)
            {
                var answer = StripMarkdown(response.Content) ?? "لم يتمكن المساعد من الإجابة.";
                await _chatStore.SaveMessageAsync(conversationId, context.UserId, "assistant", answer, AgentType, CancellationToken.None);
                return OperationResult<AgentResponse>.Success(new AgentResponse
                {
                    Text = answer,
                    SuggestedActions = GetDynamicSuggestions(lastToolCalled),
                    AdditionalData = new() { ["conversationId"] = conversationId }
                });
            }

            messages.Add(new LlmChatMessage(
                MessageRole.Assistant,
                StripMarkdown(response.Content) ?? "",
                toolCalls: response.ToolCalls));

            foreach (var call in response.ToolCalls)
            {
                _logger.LogInformation("{AgentType} calling tool: {Tool}", AgentType, call.Name);

                if (!tools.TryGetValue(call.Name, out var tool))
                {
                    messages.Add(new LlmChatMessage(MessageRole.Tool,
                        $"{{\"error\": \"الأداة '{call.Name}' غير موجودة\"}}", toolCallId: call.Id));
                    continue;
                }

                try
                {
                    lastToolCalled = call.Name;
                    var result = await tool.ExecuteAsync(call.Arguments);
                    messages.Add(new LlmChatMessage(MessageRole.Tool, result, toolCallId: call.Id));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "{AgentType} tool {Tool} failed", AgentType, call.Name);
                    messages.Add(new LlmChatMessage(MessageRole.Tool,
                        $"{{\"error\": \"{ex.Message}\"}}", toolCallId: call.Id));
                }
            }
        }

        _logger.LogError("{AgentType} exceeded max steps for conv {ConvId}", AgentType, conversationId);
        return OperationResult<AgentResponse>.Success(new AgentResponse
        {
            Text = "عذراً، لم أتمكن من إكمال العملية. يرجى إعادة صياغة سؤالك.",
            AdditionalData = new() { ["conversationId"] = conversationId }
        });
    }

    protected static string? StripMarkdown(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimStart();
            if (trimmed.StartsWith("### ")) lines[i] = lines[i].Replace("### ", "");
            else if (trimmed.StartsWith("## ")) lines[i] = lines[i].Replace("## ", "");
            else if (trimmed.StartsWith("# ")) lines[i] = lines[i].Replace("# ", "");
            lines[i] = Regex.Replace(lines[i], @"\*\*(.*?)\*\*", "$1");
            lines[i] = Regex.Replace(lines[i], @"\*(.*?)\*", "$1");
            if (Regex.IsMatch(lines[i].Trim(), @"^[-*_]{3,}$")) lines[i] = "";
            lines[i] = Regex.Replace(lines[i], @"^\s*>\s*", "");
            lines[i] = Regex.Replace(lines[i], @"`+", "");
            lines[i] = Regex.Replace(lines[i], @"🔗\s*رابط\s*(معاينة\s*)?(الامتحان\s*)?:?\s*", "");
        }

        return string.Join("\n", lines.Where(l => l != null)).Trim();
    }
}
