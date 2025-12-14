namespace AiService.Models
{
    public record ConversationMessage
    {
        public long Id { get; init; }
        public Guid ConversationId { get; init; }
        public string Role { get; init; } = default!;
        public string Content { get; init; } = default!;
        public DateTimeOffset CreatedOn { get; init; }
    }
}
