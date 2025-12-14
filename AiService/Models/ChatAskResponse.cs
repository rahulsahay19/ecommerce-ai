namespace AiService.Models
{
    public record ChatAskResponse(
        string ConversationId,
        ChatResponse Response);
}
