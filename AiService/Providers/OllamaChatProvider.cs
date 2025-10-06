
using Newtonsoft.Json.Linq;
using System.Text;

namespace AiService.Providers
{
    public sealed class OllamaChatProvider(HttpClient http, IConfiguration cfg) : IChatProvider
    {
        private readonly string _model = cfg["Ollama:ChatModel"] ?? "tinyllama";
        private readonly int _maxTokens = int.TryParse(cfg["Ollama:MaxTokens"], out var max) ? max : 150;
        private readonly double _temperature = double.TryParse(cfg["Ollama:Temperature"], out var temp) ? temp: 0.7;
        public async Task<string> ChatAsync(string prompt, CancellationToken ct = default)
        {
            var body = new
            {
                model = _model,
                prompt,
                stream = false, //Full response mode
                options = new
                {
                    num_predict = _maxTokens,
                    temperature = _temperature
                }
            };
            try
            {
                using var res = await http.PostAsJsonAsync("/api/generate", body, ct);
                res.EnsureSuccessStatusCode();

                var json = JObject.Parse(await res.Content.ReadAsStringAsync(ct));
                return json["response"]?.ToString() ?? string.Empty;
            }
            catch (TaskCanceledException tex) when(!ct.IsCancellationRequested) 
            {
                //Handling Timeout with Stream Async
                return await ChatWithStreamingAsync(prompt, ct);
            }
            catch(Exception ex)
            {
                return $"Error from Ollama Chat Provider: {ex.Message}";
            }
        }

        //Token by Token
        private async Task<string> ChatWithStreamingAsync(string prompt, CancellationToken ct)
        {
            var body = new
            {
                model = _model,
                prompt,
                stream = true, 
                options = new
                {
                    num_predict = _maxTokens,
                    temperature = _temperature
                }
            };

            using var res = await http.PostAsJsonAsync("/api/generate", body, ct);
            res.EnsureSuccessStatusCode();
            using var stream = await res.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            var sb = new StringBuilder();
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var json = JObject.Parse(line);
                if (json["response"] != null)
                {
                    sb.Append(json["response"].ToString());
                }
            }
            return sb.ToString();
        }
    }
}
