using AiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiService.Endpoints
{
    public static class ChatEndPoints
    {
        public static IEndpointRouteBuilder MapChat(this IEndpointRouteBuilder app)
        {
            app.MapPost("/chat/ask", async (
                [FromBody] ChatRequest request,
                [FromServices] IChatService chatService,
                CancellationToken ct) =>
            {
                var response = await chatService.AskAsync(request.Message, ct);
                return Results.Ok(response);
            });
            return app;
        }
        public record ChatRequest(string Message);
    }
}
