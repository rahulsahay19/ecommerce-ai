namespace AiService.Providers
{
    /// <summary>
    /// Run a web search for the given query and return summarized text results
    /// </summary>
    public interface IWebSearchProvider
    {
        Task<string> SearchAsync(string query, CancellationToken ct = default);
    }
}
