using AiService.Models;

namespace AiService.Repositories
{
    public interface IPgVectorRepository
    {
        Task InsertProductVectorAsync(
            string productId,
            string name,
            string summary,
            string description,
            string imageFile,
            string brandId,
            string brandName,
            string typeId,
            string typeName,
            decimal price,
            DateTimeOffset createdDate,
            float[] embedding);

        Task<IEnumerable<Product>> SearchByVectorAsync(float[] queryVector, int topK = 10);
        Task<IEnumerable<Product>> SearchByKeywordAsync(string keyword, int topK = 10);
        Task<IEnumerable<Product>> SearchByHybridAsync(string query, float[] queryVector, int topK = 10);
    }
}
