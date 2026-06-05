using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Providers;

public class GeminiProvider : ILLMProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<GeminiProvider> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "Gemini";

    public GeminiProvider(HttpClient http, ILogger<GeminiProvider> logger, string apiKey, string model = "gemini-2.0-flash")
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
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var contents = messages.Select(m => new
        {
            role = m.Role == "assistant" ? "model" : "user",
            parts = new[] { new { text = m.Content } }
        }).ToList();

        var body = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents,
            generationConfig = new { temperature = 0.7 }
        };

        using var res = await _http.PostAsJsonAsync(url, body, ct);
        res.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? string.Empty;
    }

    public Task<string> GenerateWithToolsAsync(string systemPrompt, string userMessage, List<AiTool> tools, CancellationToken ct = default)
    {
        return GenerateAsync(systemPrompt, userMessage, ct);
    }
}
