namespace Project.BLL.Embedding;

public interface IEmbeddingService
{
    /// <summary>توليد embedding لنص واحد (للبحث)</summary>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>توليد embeddings لعدة نصوص دفعة واحدة (للفهرسة)</summary>
    Task<float[][]> GenerateEmbeddingsBatchAsync(string[] texts, CancellationToken ct = default);
}
