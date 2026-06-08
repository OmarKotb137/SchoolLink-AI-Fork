using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Providers;

public class CloudflareAIProvider : ILLMProvider
{
    public string ProviderName => "CloudflareAI";

    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly IConfigurationSection _section;

    public CloudflareAIProvider(HttpClient http, ILogger<CloudflareAIProvider> logger, IConfiguration config)
    {
        _http = http;
        _logger = logger;
        _section = config.GetSection("LlmSettings:CloudflareAI");
    }

    private string ApiKey => _section["ApiKey"] ?? "";
    private string Model => _section["Model"] ?? "";
    private string AccountId => _section["AccountId"] ?? "";

    public async Task<string> GenerateAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var messages = new List<ChatMessage>
        {
            new() { Role = "user", Content = $"{systemPrompt}\n\n{userMessage}" }
        };
        return await GenerateChatAsync(systemPrompt, messages, ct);
    }

    public async Task<string> GenerateChatAsync(string systemPrompt, List<ChatMessage> messages, CancellationToken ct = default)
    {
        var url = $"https://api.cloudflare.com/client/v4/accounts/{AccountId}/ai/run";

        var body = new
        {
            model = Model,
            input = new
            {
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt }
                }.Concat(messages.Select(m => new { role = m.Role, content = m.Content })).ToArray()
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Add("Authorization", $"Bearer {ApiKey}");
        req.Content = JsonContent.Create(body);

        using var res = await _http.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
        {
            var errorBody = await res.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("{Provider} returned {Status}: {Body}", ProviderName, res.StatusCode, errorBody);
            return string.Empty;
        }

        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return doc.RootElement
            .GetProperty("result")
            .GetProperty("response")
            .GetString() ?? string.Empty;
    }

    public Task<string> GenerateWithToolsAsync(string systemPrompt, string userMessage, List<AiTool> tools, CancellationToken ct = default)
    {
        return GenerateAsync(systemPrompt, userMessage, ct);
    }
}