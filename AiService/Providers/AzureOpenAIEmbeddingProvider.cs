
using Newtonsoft.Json.Linq;
using static System.Net.WebRequestMethods;

namespace AiService.Providers
{
    public class AzureOpenAIEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _http;
        private readonly string _deployment;
        private readonly int _dimensions;
        private readonly string _apiVersion;

        public int Dimensions => _dimensions;

        public AzureOpenAIEmbeddingProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _deployment = cfg["AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-3-small";
            _dimensions = _deployment.Contains("large") ? 3072 : 1536;
            _apiVersion = cfg["AzureOpenAI:ApiVersion"] ?? "2024-05-01-preview";
        }

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
        {
            var vectors = await EmbedBatchAsync(new[] { text }, ct);
            return vectors[0];
        }

        public async Task<float[][]> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken ct = default)
        {
            var body = new
            {
                input = texts
            };

            for (int attempt = 0; attempt < 3; attempt++)
            {
                using var res = await _http.PostAsJsonAsync(
                $"/openai/deployments/{_deployment}/embeddings?api-version={_apiVersion}",
                body, ct);

                if (res.IsSuccessStatusCode)
                {
                    var json = JObject.Parse(await res.Content.ReadAsStringAsync(ct));

                    var vectors = json["data"]!
                            .Select(d => d["embedding"]!.Select(t => (float)t!.Value<double>()).ToArray())
                            .ToArray();
                    return vectors;
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

