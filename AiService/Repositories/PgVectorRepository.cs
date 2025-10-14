using AiService.Models;
using Dapper;
using Npgsql;

namespace AiService.Repositories
{
    public class PgVectorRepository : IPgVectorRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public PgVectorRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }
        public async Task InsertProductVectorAsync(
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
            float[] embedding)
        {
            //Decide target column based on embedding dimensions
            string embeddingColumn = embedding.Length switch
            {
                768 => "embedding_768",
                1536 => "embedding_1536",
                3072 => "embedding_3072", // future proof (Azure large model)
                _ => throw new InvalidOperationException(
                    $"Unsupported embedding size {embedding.Length}. Expected 768, 1536, or 3072.")
            };
            await using var conn = await _dataSource.OpenConnectionAsync();

            string sql = $@"
                INSERT INTO product_vectors 
                (product_id, name, summary, description, image_file, 
                 brand_id, brand_name, type_id, type_name,
                 price, created_date, {embeddingColumn})
                VALUES 
                (@ProductId, @Name, @Summary, @Description, @ImageFile,
                 @BrandId, @BrandName, @TypeId, @TypeName,
                 @Price, @CreatedDate, @Embedding)
                ON CONFLICT (product_id) DO UPDATE 
                SET {embeddingColumn} = EXCLUDED.{embeddingColumn},
                    name = EXCLUDED.name,
                    summary = EXCLUDED.summary,
                    description = EXCLUDED.description,
                    image_file = EXCLUDED.image_file,
                    brand_id = EXCLUDED.brand_id,
                    brand_name = EXCLUDED.brand_name,
                    type_id = EXCLUDED.type_id,
                    type_name = EXCLUDED.type_name,
                    price = EXCLUDED.price,
                    created_date = EXCLUDED.created_date;";

            await conn.ExecuteAsync(sql, new
            {
                ProductId = productId,
                Name = name,
                Summary = summary,
                Description = description,
                ImageFile = imageFile,
                BrandId = brandId,
                BrandName = brandName,
                TypeId = typeId,
                TypeName = typeName,
                Price = price,
                CreatedDate = createdDate,
                Embedding = embedding
            });
        }

        public async Task<IEnumerable<Product>> SearchByVectorAsync(float[] queryVector, int topK = 5)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            // Decide which embedding column to search against
            string embeddingColumn = queryVector.Length switch
            {
                768 => "embedding_768",
                1536 => "embedding_1536",
                3072 => "embedding_3072",
                _ => throw new InvalidOperationException(
                    $"Unsupported query vector size {queryVector.Length}.")
            };
            string sql = $@"
                SELECT 
                    product_id AS id,
                    name,
                    description,
                    image_file AS ""imageFile"",
                    price,
                    brand_id AS brandId,
                    brand_name AS brandName,
                    type_id AS typeId,
                    type_name AS typeName,
                    1 - ({embeddingColumn} <=> @QueryVector::vector) AS similarity
                FROM product_vectors
                ORDER BY similarity DESC
                LIMIT @TopK";

            var rows = await conn.QueryAsync(sql, new { QueryVector = queryVector, TopK = topK });
            return rows.Select(MapToProduct);

        }

        public async Task<IEnumerable<Product>> SearchByKeywordAsync(string keyword, int topK = 5)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();
            var rows = await conn.QueryAsync(@"
                SELECT 
                    product_id AS id,
                    name,
                    description,
                    image_file AS ""imageFile"",
                    price,
                    brand_id AS brandId,
                    brand_name AS brandName,
                    type_id AS typeId,
                    type_name AS typeName
                FROM product_vectors
                WHERE 
                    name ILIKE @Pattern OR
                    summary ILIKE @Pattern OR
                    description ILIKE @Pattern OR
                    brand_name ILIKE @Pattern OR
                    type_name ILIKE @Pattern
                LIMIT @TopK",
               new { Pattern = $"%{keyword}%", TopK = topK });

            return rows.Select(MapToProduct);
        }

        public async Task<IEnumerable<Product>> SearchByHybridAsync(string query, float[] queryVector, int topK = 5)
        {
            await using var conn = await _dataSource.OpenConnectionAsync();

            var keywordRows = await conn.QueryAsync(@"
                SELECT 
                    product_id AS id,
                    name,
                    description,
                    image_file AS ""imageFile"",
                    price,
                    brand_id AS brandId,
                    brand_name AS brandName,
                    type_id AS typeId,
                    type_name AS typeName
                FROM product_vectors
                WHERE name ILIKE @Query OR description ILIKE @Query
                LIMIT @TopK",
               new { Query = $"%{query}%", TopK = topK });
            if (keywordRows.Any())
            {
                return keywordRows.Select(MapToProduct);
            }
            //Otherwise fallback to vector search
            string embeddingColumn = queryVector.Length switch
            {
                768 => "embedding_768",
                1536 => "embedding_1536",
                3072 => "embedding_3072",
                _ => throw new InvalidOperationException(
                    $"Unsupported embedding size {queryVector.Length}. Expected 768, 1536 or 3072.")
            };
            var vectorRows = await conn.QueryAsync($@"
                SELECT 
                    product_id AS id,
                    name,
                    description,
                    image_file AS ""imageFile"",
                    price,
                    brand_id AS brandId,
                    brand_name AS brandName,
                    type_id AS typeId,
                    type_name AS typeName,
                    1 - ({embeddingColumn} <=> @QueryVector::vector) AS similarity
                FROM product_vectors
                ORDER BY similarity DESC
                LIMIT @TopK",
              new { QueryVector = queryVector, TopK = topK });

            return vectorRows.Select(MapToProduct);
        }


        private static Product MapToProduct(dynamic row)
        {
            return new Product(
                Id: row.id,
                Name: row.name,
                Description: row.description,
                ImageFile: row.imageFile,
                Price: row.price,
                Brand: new Brand(row.brandid, row.brandname),
                Type: new Models.Type(row.typeid, row.typename)
                );
        }

    }
}
