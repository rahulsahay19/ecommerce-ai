namespace AiService.Services
{
    public interface IVoiceService
    {
        Task<string> TranscribeAsync(IFormFile audioFile, CancellationToken ct);
        Task<byte[]> SynthesizeAsync(string text, CancellationToken ct);
    }
}
