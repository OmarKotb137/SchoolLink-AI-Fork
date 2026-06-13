namespace Project.BLL.Interfaces
{
    public interface ITextToSpeechService
    {
        Task<byte[]?> SynthesizeSpeechAsync(string text, CancellationToken ct = default);
    }
}
