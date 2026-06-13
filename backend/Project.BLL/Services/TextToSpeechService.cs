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
                var apiKey = _configuration["TextToSpeech:ApiKey"];
                var voiceName = _configuration["TextToSpeech:VoiceName"] ?? "Achird";

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
                                    text = "اتكلم باللهجة المصرية:" + text
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
                                    voiceName = voiceName
                                }
                            }
                        }
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                _logger.LogInformation("Sending TTS request to Gemini API for text of length {Length} chars.", text.Length);

                var response = await _httpClient.PostAsync(url, content, ct);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(responseJson);

                var audioBase64 = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("inlineData")
                    .GetProperty("data")
                    .GetString();

                if (string.IsNullOrEmpty(audioBase64))
                {
                    _logger.LogError("Gemini TTS response did not contain audio data.");
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
    }
}
