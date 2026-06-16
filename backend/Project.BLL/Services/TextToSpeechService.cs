using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.BLL.Interfaces;

namespace Project.BLL.Services
{
    public class TextToSpeechService : ITextToSpeechService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TextToSpeechService> _logger;

        public TextToSpeechService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<TextToSpeechService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<byte[]?> SynthesizeSpeechAsync(string text, CancellationToken ct = default)
        {
            try
            {
                // تنظيف النص من الإيموجيات والرموز غير القابلة للنطق
                text = CleanTextForTts(text);

                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("TTS text is empty after cleaning.");
                    return null;
                }

                var apiKey = _configuration["TextToSpeech:ApiKey"];

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("TextToSpeech API key is not configured.");
                    return null;
                }

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key={apiKey}";

                var payload = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new
                                {
                                    text = text
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        responseModalities = new[] { "AUDIO" },
                        speechConfig = new
                        {
                            voiceConfig = new
                            {
                                prebuiltVoiceConfig = new
                                {
                                    voiceName = "Kore"
                                }
                            }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                _logger.LogDebug("TTS payload: {Payload}", jsonPayload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending TTS request to Gemini API for text of length {Length} chars.", text.Length);

                var response = await _httpClient.PostAsync(url, content, ct);
                var responseJson = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini TTS API returned {StatusCode}: {Response}", 
                        response.StatusCode, responseJson);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Try to parse the response safely
                if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                {
                    // Check for error in response
                    if (root.TryGetProperty("error", out var errorEl))
                    {
                        var errMsg = errorEl.TryGetProperty("message", out var msgEl) 
                            ? msgEl.GetString() : "Unknown error";
                        _logger.LogError("Gemini TTS API error: {Error}", errMsg);
                    }
                    else
                    {
                        _logger.LogError("Gemini TTS response missing 'candidates'. Full response: {Response}", 
                            responseJson[..Math.Min(responseJson.Length, 500)]);
                    }
                    return null;
                }

                var candidate = candidates[0];
                if (!candidate.TryGetProperty("content", out var contentEl) ||
                    !contentEl.TryGetProperty("parts", out var parts) ||
                    parts.GetArrayLength() == 0)
                {
                    // Log full response to debug the issue
                    var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "unknown";
                    _logger.LogError("Gemini TTS response missing content/parts. finishReason={FinishReason}, full candidate: {Candidate}", 
                        finishReason, candidate.ToString());
                    // Check for promptFeedback at root
                    if (root.TryGetProperty("promptFeedback", out var pf))
                        _logger.LogError("Prompt feedback: {Feedback}", pf.ToString());
                    return null;
                }

                var part = parts[0];
                if (!part.TryGetProperty("inlineData", out var inlineData) ||
                    !inlineData.TryGetProperty("data", out var dataEl))
                {
                    // قد يكون الرد نصاً بدلاً من صوت
                    if (part.TryGetProperty("text", out var textEl))
                    {
                        _logger.LogWarning("Gemini TTS returned text instead of audio: {Text}", 
                            textEl.GetString()?[..Math.Min((textEl.GetString()?.Length ?? 0), 200)]);
                    }
                    else
                    {
                        _logger.LogError("Gemini TTS response missing inlineData. Full response: {Response}", 
                            responseJson[..Math.Min(responseJson.Length, 500)]);
                    }
                    return null;
                }

                var audioBase64 = dataEl.GetString();
                if (string.IsNullOrEmpty(audioBase64))
                {
                    _logger.LogError("Gemini TTS response contained empty audio data.");
                    return null;
                }

                var pcmData = Convert.FromBase64String(audioBase64);

                _logger.LogInformation("TTS succeeded, received {Bytes} bytes of PCM audio.", pcmData.Length);

                // Add WAV header (Gemini returns raw PCM: 16-bit, mono, 24000Hz)
                return AddWavHeader(pcmData, 24000);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("TTS request was cancelled.");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error during TTS request.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during TTS synthesis.");
                return null;
            }
        }
        /// <summary>
        /// Adds WAV header to raw PCM audio data.
        /// </summary>
        private static byte[] AddWavHeader(byte[] pcmData, int sampleRate)
        {
            const short channels = 1;
            const short bitsPerSample = 16;
            short blockAlign = (short)(channels * bitsPerSample / 8);
            int byteRate = sampleRate * blockAlign;

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + pcmData.Length); // File size - 8
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size
            writer.Write((short)1); // PCM format
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);

            // data chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(pcmData.Length);
            writer.Write(pcmData);

            return ms.ToArray();
        }

        /// <summary>
        /// تنظيف النص من الرموز غير القابلة للنطق وتحويل المعادلات الرياضية لنص عربي منطوق
        /// </summary>
        private static string CleanTextForTts(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            // 1. إزالة touch_app والنصوص غير المرغوب فيها
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\btouch_app\b", " ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 2. إزالة الإيموجيات (BMP symbols & Dingbats)
            text = System.Text.RegularExpressions.Regex.Replace(text,
                @"[\u2600-\u27BF\u2300-\u23FF\uFE00-\uFE0F\u200D]",
                " ");

            // 3. إزالة الإيموجيات خارج BMP (surrogate pairs للأيموجيات الحديثة)
            text = System.Text.RegularExpressions.Regex.Replace(text,
                @"[\uD800-\uDBFF][\uDC00-\uDFFF]",
                " ");

            // ════════════════════════════════════════════
            // تحويل الرموز الرياضية لنص عربي منطوق
            // ════════════════════════════════════════════

            // 4. تحويل الأسس: ² → تربيع, ³ → تكعيب
            text = text.Replace("\u00B2", " تربيع ");
            text = text.Replace("\u00B3", " تكعيب ");

            // 5. تحويل ^2, ^3 (إذا كانت موجودة)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s*\^\s*2\s*", " تربيع ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s*\^\s*3\s*", " تكعيب ");

            // 6. تحويل المتغير x و X إلى "إكس" (فقط لو كانت متغير وليس جزء من كلمة)
            text = System.Text.RegularExpressions.Regex.Replace(text,
                @"(?<=^|[^a-zA-Z\u0600-\u06FF])x(?=$|[^a-zA-Z\u0600-\u06FF])",
                " إكس ", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // 7. تحويل علامات العمليات الحسابية
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s*=\s*", " يساوي ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s*\+\s*", " زائد ");
            text = text.Replace("\u00D7", " في ");   // علامة × → في
            text = text.Replace("\u00F7", " على ");  // علامة ÷ → على
            text = text.Replace("\u2212", " ناقص "); // علامة − ناقص (minus sign)

            // 8. تحويل شرطة - إلى "ناقص" في السياقات الرياضية
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+-\s+", " ناقص ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"-(\d)", " ناقص $1");

            // 9. إزالة الأقواس (استبدالها بمسافات)
            text = text.Replace("(", " ");
            text = text.Replace(")", " ");

            // 10. إزالة الأسهم (مثل ← →)
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[\u2190-\u21FF]", " ");

            // 11. إزالة الشرطات الطويلة
            text = text.Replace("\u2014", " ");
            text = text.Replace("\u2013", " ");
            text = text.Replace("\u2026", " ... ");

            // 12. إزالة أي رموز متبقية غير عربية أو لاتينية
            text = System.Text.RegularExpressions.Regex.Replace(text,
                @"[^\u0600-\u06FF" +         // العربية
                @"\u0660-\u0669" +           // أرقام عربية
                @"\u0030-\u0039" +           // أرقام غربية
                @"\u0041-\u005A" +           // English uppercase
                @"\u0061-\u007A" +           // English lowercase
                @"\u0020\u002C\u002E" +      // مسافة، فاصلة، نقطة
                @"\u061F\u060C\u061B" +      // ؟ ، ؛
                @"\u0021\u003F\u003A" +      // ! ? :
                @"]", " ");

            // 13. إزالة الـ zero-width characters
            text = System.Text.RegularExpressions.Regex.Replace(text, @"[\u200B-\u200F\u2028-\u202F\uFEFF]", "");

            // 14. ضغط المسافات المتعددة
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

            return text.Trim();
        }
    }
}
