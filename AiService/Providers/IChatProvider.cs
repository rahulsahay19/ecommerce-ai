namespace AiService.Providers
{
    public interface IChatProvider
    {
        Task<string> ChatAsync(string prompt, CancellationToken ct = default);
    }
}
