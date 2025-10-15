
using System.Net.Http;
using System.Text.Json;

namespace AiService.Providers
{
    public class OpenAIChatProvider : IChatProvider
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public OpenAIChatProvider(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }
        public async Task<string> ChatAsync(string prompt, CancellationToken ct = default)
        {
            var request = new
            {
                model = _config["OpenAI:ChatModel"] ?? "gpt-4o-mini",
                messages = new[]
             {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                },
                max_tokens = int.TryParse(_config["OpenAI:MaxTokens"], out var mt) ? mt : 200,
                temperature = double.TryParse(_config["OpenAI:Temperature"], out var temp) ? temp : 0.7
            };

            var response = await _httpClient.PostAsJsonAsync("/chat/completions", request, ct);

            if (!response.IsSuccessStatusCode)
            { 
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"OpenAI Chat API failed: {error}");
            }
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = doc.RootElement;
  
            return root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
                ?? string.Empty;
        }
    }
}
