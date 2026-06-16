using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Project.BLL.Embedding;

public class HuggingFaceEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HuggingFaceEmbeddingService> _logger;
    private readonly string _apiKey;
    private readonly string _embeddingUrl;
    private const string PassagePrefix = "passage: ";
    private const string QueryPrefix = "query: ";

    public HuggingFaceEmbeddingService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<HuggingFaceEmbeddingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _apiKey = configuration["LlmSettings:HuggingFace:ApiKey"] ?? "";
        _embeddingUrl = configuration["LlmSettings:HuggingFace:EmbeddingUrl"]
            ?? "https://api-inference.huggingface.co/pipeline/feature-extraction/BAAI/bge-m3";
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var results = await GenerateEmbeddingsBatchAsync([text], ct);
        return results.Length > 0 ? results[0] : Array.Empty<float>();
    }

    public async Task<float[][]> GenerateEmbeddingsBatchAsync(string[] texts, CancellationToken ct = default)
    {
        if (texts.Length == 0) return [];

        var request = new HttpRequestMessage(HttpMethod.Post, _embeddingUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new { inputs = texts.Select(t => $"{PassagePrefix}{t}").ToArray() };
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<float[][]>(json);

        if (result is null || result.Length != texts.Length)
        {
            _logger.LogWarning("Embedding response mismatch: expected {Count}, got {Actual}", texts.Length, result?.Length ?? 0);
            return [];
        }

        // Normalize vectors (L2)
        for (int i = 0; i < result.Length; i++)
            result[i] = NormalizeVector(result[i]);

        return result;
    }

    /// <summary>توليد embedding لاستعلام البحث (query prefix)</summary>
    public async Task<float[]> GenerateQueryEmbeddingAsync(string query, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _embeddingUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var payload = new { inputs = new[] { $"{QueryPrefix}{query}" } };
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            System.Text.Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<float[][]>(json);

        if (result is null || result.Length == 0) return [];

        return NormalizeVector(result[0]);
    }

    private static float[] NormalizeVector(float[] vector)
    {
        float sumSq = 0;
        for (int i = 0; i < vector.Length; i++)
            sumSq += vector[i] * vector[i];

        float norm = MathF.Sqrt(sumSq);
        if (norm < 1e-10f) return vector;

        for (int i = 0; i < vector.Length; i++)
            vector[i] /= norm;

        return vector;
    }
}
