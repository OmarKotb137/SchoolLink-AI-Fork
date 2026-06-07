using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Providers;

public abstract class OpenAICompatibleProvider : ILLMProvider
{
    public abstract string ProviderName { get; }

    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly IConfigurationSection _section;

    protected OpenAICompatibleProvider(HttpClient http, ILogger logger, IConfiguration config, string sectionName)
    {
        _http = http;
        _logger = logger;
        _section = config.GetSection($"LlmSettings:{sectionName}");
    }

    protected string ApiKey => _section["ApiKey"] ?? "";
    protected virtual string Model => _section["Model"] ?? "";
    protected string BaseUrl => _section["BaseUrl"] ?? "";
    protected string this[string key] => _section[key] ?? "";

    protected abstract string Endpoint { get; }

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
        var body = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt }
            }.Concat(messages.Select(m => new { role = m.Role, content = m.Content })).ToArray(),
            stream = false
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
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
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    public Task<string> GenerateWithToolsAsync(string systemPrompt, string userMessage, List<AiTool> tools, CancellationToken ct = default)
    {
        return GenerateAsync(systemPrompt, userMessage, ct);
    }
}
