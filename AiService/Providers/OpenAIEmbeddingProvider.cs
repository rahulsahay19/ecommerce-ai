
using Newtonsoft.Json.Linq;

namespace AiService.Providers
{
    public sealed class OpenAIEmbeddingProvider(HttpClient http, IConfiguration cfg) : IEmbeddingProvider
    {
        private readonly string _model = cfg["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
        private readonly int _dimensions = int.TryParse(cfg["OpenAI:Dimensions"], out var d) ? d : 1536;

        public int Dimensions => _dimensions;

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            var vectors = await EmbedBatchAsync(new[] { text }, ct);
            return vectors[0];
        }

        public async Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
        {
            var body = new
            {
                input = texts,
                model = _model
            };

            for (int attempt = 0; attempt < 3; attempt++)
            {
                using var res = await http.PostAsJsonAsync("embeddings", body, ct);
                if (res.IsSuccessStatusCode)
                {
                    var json = JObject.Parse(await res.Content.ReadAsStringAsync(ct));

                    var vectors = json["data"]!
                            .Select(d => d["embedding"]!.Select(t => (float)t!.Value<double>()).ToArray())
                            .ToArray();
                }
                if ((int)res.StatusCode == 429) // too many requests
                {
                    var retryAfter = res.Headers.RetryAfter?.Delta?.TotalSeconds ?? (2 * (attempt + 1));
                    await Task.Delay(TimeSpan.FromSeconds(retryAfter), ct);
                    continue; //retry
                }
                res.EnsureSuccessStatusCode();
            }
            throw new HttpRequestException("Exceeded max tries due to rate limiting (429");
        }
    }
}
