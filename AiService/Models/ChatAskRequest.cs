namespace AiService.Models
{
    public record ChatAskRequest(
        string UserQuery,
        string? ConversationId,
        int MaxHistoryTurns = 8); 
}
