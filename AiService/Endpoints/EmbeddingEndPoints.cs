using AiService.Providers;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Endpoints
{
    public static class EmbeddingEndPoints
    {
        public static IEndpointRouteBuilder MapEmbeddingTest(this IEndpointRouteBuilder app) 
        {
            app.MapPost("/embeddings/test", async (
                [FromServices] IEmbeddingProvider provider,
                TestReq req, CancellationToken ct) =>
            {
                var vector = await provider.EmbedAsync(req.Text ?? "", ct);
                return Results.Ok(new
                {
                    provider = provider.GetType().Name,
                    dimensions = provider.Dimensions,
                    length = vector.Length
                });
            });
            return app;
        }
        public record TestReq(string Text);
    }
}
