namespace AiService.Models
{
    public record SourceResult(
        string Type, // db, web
        string Content,
        string? Url = null
        );
}
