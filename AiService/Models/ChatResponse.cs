namespace AiService.Models
{
    public record ChatResponse(
            string Answer,
            IEnumerable<Product>? Products = null,
            IEnumerable<SourceResult>? Sources = null
        );
    
}
