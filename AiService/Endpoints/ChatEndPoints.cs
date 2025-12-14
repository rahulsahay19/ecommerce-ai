using AiService.Models;
using AiService.Repositories;
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

            //Context aware endpoint

            app.MapPost("/chat/ask/context", async (
            [FromBody] ChatAskRequest request,
            [FromServices] IChatService chatService,
            [FromServices] IConversationRepository conversationRepository,
            CancellationToken ct) =>
                {
                    // 1. Resolve conversationId
                    Guid conversationId;

                    if (!string.IsNullOrWhiteSpace(request.ConversationId) &&
                        Guid.TryParse(request.ConversationId, out var parsedId) &&
                        await conversationRepository.ConversationExistsAsync(parsedId, ct))
                    {
                        conversationId = parsedId;
                    }
                    else
                    {
                        conversationId = await conversationRepository.CreateConversationAsync(ct);
                    }

                    // 2. Load conversation history
                    var messages = await conversationRepository.GetRecentMessagesAsync(
                        conversationId,
                        request.MaxHistoryTurns * 2,
                        ct);

                    var chatHistory = messages
                        .Select(m => (m.Role, m.Content))
                        .ToList();

                    // 3. Ground the query 
                    var effectiveQuery = request.UserQuery;

                    bool isFollowUp =
                        messages.Any() &&
                        request.UserQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 6;

                    var lastTopicQuery = messages
                    .Where(m => m.Role == "user")
                    .Select(m => m.Content)
                    .Reverse()
                    .FirstOrDefault(IsDomainQuery);


                    if (isFollowUp && !string.IsNullOrWhiteSpace(lastTopicQuery))
                    {
                        effectiveQuery = $"{lastTopicQuery}. {request.UserQuery}";
                    }

                    // 4. Call chat service WITH GROUNDED QUERY
                    var response = await chatService.AskAsync(
                        effectiveQuery,
                        ct,
                        chatHistory);

                    // 5. Persist messages
                    await conversationRepository.AppendMessageAsync(conversationId, "user", request.UserQuery, ct);
                    await conversationRepository.AppendMessageAsync(conversationId, "assistant", response.Answer, ct);

                    // 6. Return
                    return Results.Ok(new ChatAskResponse(
                        ConversationId: conversationId.ToString(),
                        Response: response));
                });

            return app;
        }

        static bool IsDomainQuery(string q)
        {
            q = q.ToLowerInvariant();

            return
                q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length >= 2 &&
                !q.Contains("cheaper") &&
                !q.Contains("costliest") &&
                !q.Contains("under") &&
                !q.Contains("above") &&
                !q.Contains("which") &&
                !q.Contains("first") &&
                !q.Contains("second") &&
                !q.Contains("that") &&
                !q.Contains("this");
        }

        public record ChatRequest(string Message);
    }
}
