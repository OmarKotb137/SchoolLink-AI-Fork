using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Providers;

public class DeepSeekProvider : ILLMProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<DeepSeekProvider> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "DeepSeek";

    public DeepSeekProvider(HttpClient http, ILogger<DeepSeekProvider> logger, string apiKey, string model = "deepseek-chat")
    {
        _http = http;
        _logger = logger;
        _apiKey = apiKey;
        _model = model;
    }

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
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt }
            }.Concat(messages.Select(m => new { role = m.Role, content = m.Content })).ToArray(),
            temperature = 0.7,
            max_tokens = 4096
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.deepseek.com/v1/chat/completions");
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Content = JsonContent.Create(body);

        using var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();

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
