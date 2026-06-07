using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Infrastructure;

public class CloudflareAILlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _accountId;
    private readonly ILogger<CloudflareAILlmClient> _logger;

    public CloudflareAILlmClient(HttpClient http, IConfiguration config, ILogger<CloudflareAILlmClient> logger)
    {
        _http = http;
        _logger = logger;
        _model = config["LlmSettings:CloudflareAI:Model"]
                 ?? "@cf/meta/llama-3.1-8b-instruct";
        _accountId = config["LlmSettings:CloudflareAI:AccountId"]
                     ?? throw new InvalidOperationException("CloudflareAI AccountId is missing");
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

        var body = new JsonObject
        {
            ["model"] = _model,
            ["input"] = new JsonObject
            {
                ["messages"] = msgsArray
            }
        };

        var url = $"https://api.cloudflare.com/client/v4/accounts/{_accountId}/ai/run";
        var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        _logger.LogInformation("CloudflareAI Workers AI request: {Url} model={Model}", url, _model);
        var resp = await _http.PostAsync(url, content);
        if (!resp.IsSuccessStatusCode)
        {
            var errorBody = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("CloudflareAI Workers AI returned {Status}: {Body}", resp.StatusCode, errorBody);
        }
        resp.EnsureSuccessStatusCode();

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var resultObj = doc.RootElement.GetProperty("result");

        var result = new LlmResponse();

        if (resultObj.TryGetProperty("response", out var textEl) && textEl.ValueKind != JsonValueKind.Null)
            result.Content = textEl.GetString();

        if (resultObj.TryGetProperty("tool_calls", out var callsEl) && callsEl.ValueKind == JsonValueKind.Array)
        {
            result.ToolCalls = new List<ToolCall>();
            foreach (var call in callsEl.EnumerateArray())
            {
                result.ToolCalls.Add(new ToolCall
                {
                    Id = call.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "",
                    Name = call.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "",
                    Arguments = call.TryGetProperty("arguments", out var argsEl) ? argsEl.GetString() ?? "{}" : "{}"
                });
            }
        }

        return result;
    }
}
