using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;

namespace Project.BLL.AI.Providers;

public class OpenRouterProvider : ILLMProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<OpenRouterProvider> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public string ProviderName => "OpenRouter";

    private const string Endpoint = "https://openrouter.ai/api/v1/chat/completions";

    public OpenRouterProvider(HttpClient http, ILogger<OpenRouterProvider> logger, string apiKey, string model = "openrouter/owl-alpha")
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
            temperature = 0.3,
            max_tokens = 8192
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, Endpoint);
        req.Headers.Add("Authorization", $"Bearer {_apiKey}");
        req.Headers.Add("HTTP-Referer", "https://schoollink.ai");
        req.Headers.Add("X-Title", "SchoolLink AI");
        req.Content = JsonContent.Create(body);

        _logger.LogInformation("Calling OpenRouter model {Model}", _model);

        using var res = await _http.SendAsync(req, ct);
        var responseBody = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenRouter returned {Status}: {Body}", res.StatusCode, responseBody);
            return string.Empty;
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return StripMarkdownFences(content.Trim());
    }

    public Task<string> GenerateWithToolsAsync(string systemPrompt, string userMessage, List<AiTool> tools, CancellationToken ct = default)
    {
        return GenerateAsync(systemPrompt, userMessage, ct);
    }

    private static string StripMarkdownFences(string content)
    {
        if (content.StartsWith("```markdown", StringComparison.OrdinalIgnoreCase) ||
            content.StartsWith("```"))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.LastIndexOf("```");
            if (start > 0 && end > start)
                return content[start..end].Trim();
        }
        return content;
    }
}

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
