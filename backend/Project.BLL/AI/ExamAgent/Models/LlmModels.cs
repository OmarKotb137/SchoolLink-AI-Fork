using System.Text.Json;

namespace Project.BLL.AI.ExamAgent.Models;

public enum MessageRole { System, User, Assistant, Tool }

public class LlmChatMessage
{
    public MessageRole Role { get; set; }
    public string Content { get; set; } = "";
    public string? ToolCallId { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }

    public LlmChatMessage() { }
    public LlmChatMessage(MessageRole role, string content, string? toolCallId = null, List<ToolCall>? toolCalls = null)
    {
        Role = role; Content = content; ToolCallId = toolCallId; ToolCalls = toolCalls;
    }
}

public class ToolCall
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Arguments { get; set; } = "{}";
}

public class LlmResponse
{
    public string? Content { get; set; }
    public List<ToolCall>? ToolCalls { get; set; }
}

public class FunctionDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public object InputSchema { get; set; } = new();
}

public class ToolResult
{
    public bool Success { get; set; }
    public object? Data { get; set; }
    public string? Error { get; set; }

    public static ToolResult Ok(object data) => new() { Success = true, Data = data };
    public static ToolResult Fail(string error) => new() { Success = false, Error = error };

    public string ToJson()
    {
        if (Success) return JsonSerializer.Serialize(Data);
        return JsonSerializer.Serialize(new { error = Error });
    }
}
