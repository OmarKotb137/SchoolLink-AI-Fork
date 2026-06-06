using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;

namespace Project.BLL.AI.ExamAgent.Infrastructure;

public class OpenCodeAILlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _baseUrl;

    public OpenCodeAILlmClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["LlmSettings:OpenCodeAI:Model"]
                 ?? "deepseek-v4-flash-free";
        _baseUrl = config["LlmSettings:OpenCodeAI:BaseUrl"]
                   ?? "https://opencode.ai/zen/v1";
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

            if (m.ToolCalls is not null && m.ToolCalls.Count > 0)
            {
                var toolCallsArray = new JsonArray();
                foreach (var call in m.ToolCalls)
                {
                    toolCallsArray.Add(new JsonObject
                    {
                        ["id"] = call.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = call.Name,
                            ["arguments"] = call.Arguments
                        }
                    });
                }
                node["tool_calls"] = toolCallsArray;
            }

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
            ["tools"] = toolsArray,
            ["stream"] = false,
            ["tool_choice"] = "auto",
            ["max_tokens"] = 4096
        };

        var url = $"{_baseUrl.TrimEnd('/')}/chat/completions";

        var jsonString = body.ToJsonString();
        var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        var resp = await _http.PostAsync(url, content);

        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"OpenCodeAI API error {resp.StatusCode}: {errorBody}");
        }

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
