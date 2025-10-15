
using AiService.Models;

namespace AiService.Providers
{
    public class AzureOpenAIChatProvider : IChatProvider
    {
        private readonly HttpClient _http;
        private readonly string _deployment;

        public AzureOpenAIChatProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _deployment = cfg["AzureOpenAI:ChatDeployment"] ?? "gpt-4o-mini";
        }
        public async Task<string> ChatAsync(string prompt, CancellationToken ct = default)
        {
            var body = new
            {
                messages = new[]
            {
                new { role = "system", content =
                    "You are an AI shopping assistant for an e-commerce platform. " +
                    "Help users find products, compare prices, and suggest related items. " +
                    "If a user asks a query, use product catalog context if available. " +
                    "Always answer politely and concisely." },

                new { role = "user", content = prompt }
            },
                max_tokens = 500
            };
            var res = await _http.PostAsJsonAsync(
           $"/openai/deployments/{_deployment}/chat/completions?api-version=2024-05-01-preview",
           body, ct);

            res.EnsureSuccessStatusCode();
            var json = await res.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);
            return json?.choices.FirstOrDefault()?.message?.content ?? "No Response";
        }
    }
}
