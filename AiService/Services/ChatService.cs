namespace AiService.Services
{
    using AiService.Models;
    using AiService.Providers;
    using AiService.Repositories;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class ChatService : IChatService
    {
        private readonly IPgVectorRepository _repo;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IChatProvider _chatProvider;
        private readonly IWebSearchProvider _webSearchProvider;

        public ChatService(
            IPgVectorRepository repo,
            IEmbeddingProvider embeddingProvider,
            IChatProvider chatProvider,
            IWebSearchProvider webSearchProvider)
        {
            _repo = repo;
            _embeddingProvider = embeddingProvider;
            _chatProvider = chatProvider;
            _webSearchProvider = webSearchProvider;
        }

        public async Task<ChatResponse> AskAsync(
            string userQuery,
            CancellationToken cancellationToken = default,
            IEnumerable<(string Role, string Content)>? chatHistory = null)
        {
            // 0. Handle greetings / small talk early
            var normalized = userQuery.Trim().ToLower();
            if (normalized is "hi" or "hello" or "hey")
            {
                return new ChatResponse(
                    Answer: "Hello 👋, how can I help you today?",
                    Products: null,
                    Sources: new List<SourceResult>
                    {
                new SourceResult("system", "Greeting fallback")
                    }
                );
            }

            //1. Embed the Query
            var queryVector = await _embeddingProvider.EmbedAsync(userQuery, cancellationToken);

            //2. Try Hybrid Search
            var results = await _repo.SearchByHybridAsync(userQuery, queryVector, 5);

            string conversationalContext = BuildConversationContext(chatHistory);

            //3. If Products found
            if (results.Any()) 
            {
                // Build product context for the LLM
                var productContext = string.Join("\n",
                    results.Select(r => $"{r.Name} - ₹{r.Price} ({r.Brand?.Name})"));

                var prompt = $@"
                User asked: {userQuery}

                Relevant products (for your reference only):
                {string.Join("\n", results.Select(r => $"{r.Name} - ₹{r.Price} ({r.Brand?.Name})"))}

                Instruction: Do not list products in your answer. Just respond conversationally (e.g., 'Here are some products I found for you').
                ";

                //    "Answer conversationally and recommend products in **English only**.";

                var answer = await _chatProvider.ChatAsync(prompt, cancellationToken);

                return new ChatResponse(
                    Answer: answer,
                    Products: results,   // already IEnumerable<Product>
                    Sources: new List<SourceResult>
                    {
            new SourceResult("db", "Results fetched from product database")
                    }
                );
            }

            // 4. Fallback to web search
            var webResults = await _webSearchProvider.SearchAsync(userQuery, cancellationToken);
            var fallbackPrompt = $"{conversationalContext}\nUser asked: {userQuery}\nI couldn’t find results in DB. Here is what I found on the web:\n{webResults}\n\nSummarize helpfully.";
            var fallbackAnswer = await _chatProvider.ChatAsync(fallbackPrompt, cancellationToken);

            return new ChatResponse(
                Answer: fallbackAnswer,
                Products: null,
                Sources: new List<SourceResult>
                {
                    new SourceResult("web", webResults)
                }
            );

        }

        private string BuildConversationContext(IEnumerable<(string Role, string Content)>? chatHistory)
        {
            if (chatHistory == null) return string.Empty;
            return string.Join("\n", chatHistory.Select(h => $"{h.Role}: {h.Content}"));
        }
    }
}
