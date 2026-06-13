using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Infrastructure;

public class OpenCodeAILlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly int _maxTokens;

    public OpenCodeAILlmClient(HttpClient http, IConfiguration config)
    {
        _http = http;
        _model = config["LlmSettings:OpenCodeAI:Model"]
                 ?? "deepseek-v4-flash-free";
        _baseUrl = config["LlmSettings:OpenCodeAI:BaseUrl"]
                   ?? "https://opencode.ai/zen/v1";
        _maxTokens = int.TryParse(config["LlmSettings:OpenCodeAI:MaxTokens"], out var mt) ? mt : 16384;
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

        var body = new JsonObject
        {
            ["model"] = _model,
            ["messages"] = msgsArray,
            ["stream"] = false,
            ["max_tokens"] = _maxTokens
        };

        if (tools.Any())
        {
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
            body["tools"] = toolsArray;
            body["tool_choice"] = "auto";
        }

        var url = $"{_baseUrl.TrimEnd('/')}/chat/completions";
        var jsonString = body.ToJsonString();

        // Retry up to 2 times with exponential backoff for transient failures
        var maxRetries = 2;
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = new StringContent(jsonString, Encoding.UTF8, "application/json"),
                    Version = new Version(1, 1),
                    VersionPolicy = HttpVersionPolicy.RequestVersionExact
                };

                var resp = await _http.SendAsync(request);

                if (!resp.IsSuccessStatusCode)
                {
                    var errorBody = await resp.Content.ReadAsStringAsync();

                    // Handle rate limits gracefully
                    if (resp.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                        errorBody.Contains("FreeUsageLimitError") ||
                        errorBody.Contains("Rate limit"))
                    {
                        return new LlmResponse
                        {
                            Content = "عذراً، تم تجاوز حد الاستخدام المسموح به حالياً. يرجى الانتظار قليلاً ثم المحاولة مرة أخرى. 🙏"
                        };
                    }

                    // For other errors, also return friendly message
                    return new LlmResponse
                    {
                        Content = "عذراً، حدث خطأ في الاتصال بالمساعد الذكي. يرجى المحاولة مرة أخرى لاحقاً."
                    };
                }

                var jsonResp = await resp.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(jsonResp);
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
            catch (Exception ex) when (attempt < maxRetries &&
                (ex is HttpRequestException || ex is TaskCanceledException || ex is System.IO.IOException))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt + 1));
                await Task.Delay(delay);
            }
        }

        throw new HttpRequestException("فشل الاتصال بعد 3 محاولات");
    }
}
