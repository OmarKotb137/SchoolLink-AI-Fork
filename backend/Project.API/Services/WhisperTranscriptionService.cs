using NAudio.Wave;
using Whisper.net;

namespace Project.API.Services;

public sealed class WhisperTranscriptionService : IDisposable
{
    private readonly string _modelPath;
    private readonly ILogger<WhisperTranscriptionService> _logger;

    public WhisperTranscriptionService(ILogger<WhisperTranscriptionService> logger)
    {
        var appDir = Path.GetDirectoryName(typeof(WhisperTranscriptionService).Assembly.Location)!;
        _modelPath = Path.Combine(appDir, "ai", "LocalModel", "ggml-base.bin");
        _logger = logger;
    }

    public async Task<string> TranscribeAsync(string audioFilePath)
    {
        if (!System.IO.File.Exists(_modelPath))
        {
            _logger.LogError("Whisper model not found at: {Path}", _modelPath);
            throw new FileNotFoundException($"Whisper model not found at: {_modelPath}");
        }

        var wavPath = Path.Combine(Path.GetTempPath(), $"whisper_{Guid.NewGuid():N}.wav");

        try
        {
            ConvertToWav(audioFilePath, wavPath);

            using var factory = WhisperFactory.FromPath(_modelPath);
            using var processor = factory.CreateBuilder().WithLanguage("ar").Build();

            var segments = new List<string>();
            await using var wavStream = System.IO.File.OpenRead(wavPath);
            await foreach (var result in processor.ProcessAsync(wavStream))
            {
                var text = (result.Text ?? "").Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    segments.Add(text);
            }

            var combined = string.Join(" ", segments);
            _logger.LogInformation("Transcription completed: {Length} chars", combined.Length);
            return combined;
        }
        finally
        {
            if (System.IO.File.Exists(wavPath))
                System.IO.File.Delete(wavPath);
        }
    }

    private static void ConvertToWav(string inputPath, string outputWavPath)
    {
        using var reader = new AudioFileReader(inputPath);
        var outFormat = new WaveFormat(16000, 16, 1);
        using var resampler = new MediaFoundationResampler(reader, outFormat)
        {
            ResamplerQuality = 60
        };
        WaveFileWriter.CreateWaveFile(outputWavPath, resampler);
    }

    public void Dispose() { }
}
