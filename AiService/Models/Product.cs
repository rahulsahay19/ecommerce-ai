namespace AiService.Models
{
    public record Product(
        string Id,
        string Name,
        string Description,
        string ImageFile,
        decimal Price,
        Brand Brand,
        Type Type
        );
}
