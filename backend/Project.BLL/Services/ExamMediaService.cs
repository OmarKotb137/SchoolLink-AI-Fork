using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Project.BLL.Interfaces;

namespace Project.BLL.Services
{
    public class ExamMediaService : IExamMediaService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ExamMediaService> _logger;
        private readonly string _apiKey;
        private readonly string _mediaFolder;

        public ExamMediaService(
            HttpClient http,
            ILogger<ExamMediaService> logger,
            string apiKey,
            string mediaFolder)
        {
            _http = http;
            _logger = logger;
            _apiKey = apiKey;
            _mediaFolder = mediaFolder;
        }

        public async Task<string> GenerateImageAsync(
            string imagePrompt, int groupId, CancellationToken ct = default)
        {
            var safePrompt =
                $"{imagePrompt}. Strictly black and white, clean line art, " +
                "no shading, no color, high contrast, suitable for printing on a paper exam.";

            var body = new
            {
                model = "dall-e-3",
                prompt = safePrompt,
                n = 1,
                size = "1024x1024",
                quality = "standard",
                response_format = "b64_json"
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
            req.Headers.Add("Authorization", $"Bearer {_apiKey}");
            req.Content = JsonContent.Create(body);

            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            var b64 = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("b64_json")
                .GetString()!;

            var bytes = Convert.FromBase64String(b64);

            var fileName = $"group-{groupId}-{Guid.NewGuid():N}.png";
            var fullPath = Path.Combine(_mediaFolder, fileName);
            Directory.CreateDirectory(_mediaFolder);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);

            _logger.LogInformation("Generated exam image for group {GroupId}: {File}", groupId, fileName);

            return $"/exam-media/{fileName}";
        }

        public string SanitizeSvg(string rawSvg)
        {
            if (string.IsNullOrWhiteSpace(rawSvg))
                throw new ArgumentException("Empty SVG content.");

            var cleaned = Regex.Replace(rawSvg, @"<script[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\bon\w+\s*=\s*""[^""]*""", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\bon\w+\s*=\s*'[^']*'", "", RegexOptions.IgnoreCase);

            if (!cleaned.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Content is not valid SVG.");

            return cleaned;
        }
    }
}
