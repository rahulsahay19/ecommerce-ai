
using Newtonsoft.Json.Linq;

namespace AiService.Providers
{
    public class OllamaEmbeddingProvider(HttpClient http, IConfiguration cfg) : IEmbeddingProvider
    {
        private readonly string _model = cfg["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
        public int Dimensions => int.TryParse(cfg["Ollama:Dimensions"], out var d) ? d : 768;

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            var body = new {model = _model, prompt = text};
            using var res = await http.PostAsJsonAsync("/api/embeddings", body, ct);
            res.EnsureSuccessStatusCode();
            var json = JObject.Parse(await res.Content.ReadAsStringAsync()); //[0.2345, -0.1234, 0.8765, ...]
            var arr = json["embedding"]!.Select(t => (float)t!.Value<double>()).ToArray();
            return arr;
        }

        public async Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
        {
            var results = new List<float[]>();
            foreach (var text in texts) 
            {
                var body = new { model = _model, prompt = text };
                using var res = await http.PostAsJsonAsync("/api/embeddings", body, ct);
                res.EnsureSuccessStatusCode();
                var json = JObject.Parse(await res.Content.ReadAsStringAsync()); //[0.2345, -0.1234, 0.8765, ...]
                var arr = json["embedding"]!.Select(t => (float)t!.Value<double>()).ToArray();
                results.Add(arr);
            }
            return results.ToArray();
        }
    }
}
