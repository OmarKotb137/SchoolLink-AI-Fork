using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Infrastructure;

public class HuggingFaceLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _baseUrl;

    public HuggingFaceLlmClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["LlmSettings:HuggingFace:Model"] ?? "Qwen/Qwen3-32B:groq";
        _baseUrl = config["LlmSettings:HuggingFace:BaseUrl"]
                   ?? "https://router.huggingface.co/v1";
    }

    public async Task<LlmResponse> ChatAsync(
        List<LlmChatMessage> messages,
        IEnumerable<FunctionDefinition> tools)
    {
        var msgsArray = new JsonArray();
        foreach (var m in messages)
        {
            var role = m.Role switch
            {
                MessageRole.System => "system",
                MessageRole.User => "user",
                MessageRole.Assistant => "assistant",
                MessageRole.Tool => "tool",
                _ => "user"
            };

            var node = new JsonObject
            {
                ["role"] = role,
                ["content"] = m.Content
            };
            if (m.ToolCallId is not null)
                node["tool_call_id"] = m.ToolCallId;

            msgsArray.Add(node);
        }

        var toolsArray = new JsonArray();
        foreach (var t in tools)
        {
            toolsArray.Add(new JsonObject
            {
                ["type"] = "function",
                ["function"] = new JsonObject
                {
                    ["name"] = t.Name,
                    ["description"] = t.Description,
                    ["parameters"] = JsonNode.Parse(JsonSerializer.Serialize(t.InputSchema))
                }
            });
        }

        var body = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = msgsArray,
            ["stream"] = false
        };

        var url = $"{_baseUrl.TrimEnd('/')}/chat/completions";
        var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync(url, content);
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var choice = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

        var result = new LlmResponse();

        if (choice.TryGetProperty("content", out var textEl) && textEl.ValueKind != JsonValueKind.Null)
            result.Content = textEl.GetString();

        if (choice.TryGetProperty("tool_calls", out var callsEl) && callsEl.ValueKind == JsonValueKind.Array)
        {
            result.ToolCalls = new List<ToolCall>();
            foreach (var call in callsEl.EnumerateArray())
            {
                result.ToolCalls.Add(new ToolCall
                {
                    Id = call.GetProperty("id").GetString() ?? "",
                    Name = call.GetProperty("function").GetProperty("name").GetString() ?? "",
                    Arguments = call.GetProperty("function").GetProperty("arguments").GetString() ?? "{}"
                });
            }
        }

        return result;
    }
}
