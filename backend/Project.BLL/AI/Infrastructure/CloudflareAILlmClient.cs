using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Infrastructure;

public class CloudflareAILlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _accountId;
    private readonly string _gateway;
    private readonly string _baseUrl;

    public CloudflareAILlmClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["LlmSettings:CloudflareAI:Model"]
                 ?? "workers-ai/@cf/meta/llama-3.1-8b-instruct";
        _accountId = config["LlmSettings:CloudflareAI:AccountId"]
                     ?? throw new InvalidOperationException("CloudflareAI AccountId is missing");
        _gateway = config["LlmSettings:CloudflareAI:Gateway"] ?? "default";
        _baseUrl = config["LlmSettings:CloudflareAI:BaseUrl"]
                   ?? "https://gateway.ai.cloudflare.com/v1";
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
            ["tools"] = toolsArray,
            ["stream"] = false,
            ["tool_choice"] = "auto"
        };

        var url = $"{_baseUrl.TrimEnd('/')}/{_accountId}/{_gateway}/compat/chat/completions";

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
