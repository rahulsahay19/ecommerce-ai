using AiService.Models;

namespace AiService.Repositories
{
    public interface IConversationRepository
    {
        Task<Guid> CreateConversationAsync(CancellationToken ct = default);
        Task<bool> ConversationExistsAsync(Guid conversationId, CancellationToken ct = default);
        Task AppendMessageAsync(Guid conversationId, string role, string content, CancellationToken ct = default);

        Task<IReadOnlyList<ConversationMessage>> GetRecentMessagesAsync(
            Guid conversationId,
            int take,
            CancellationToken ct = default);
    }
}
