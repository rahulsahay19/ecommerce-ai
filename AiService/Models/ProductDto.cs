namespace AiService.Models
{
    public record ProductDto
    (
        string Id,
        string Name,
        string Summary,
        string Description,
        string ImageFile,
        ProductBrand Brand,
        ProductType Type,
        decimal Price,
        DateTimeOffset CreatedDate
       );
}
