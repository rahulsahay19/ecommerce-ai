namespace AiService.Models
{
    public record CatalogResponse(int PageIndex, int PageSize, int Count, List<ProductDto> Data);   
}
