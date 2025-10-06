namespace AiService.Providers
{
    public interface IEmbeddingProvider
    {
        Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
        Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default);
        int Dimensions { get; } // eg 768, 1536
    }
}
