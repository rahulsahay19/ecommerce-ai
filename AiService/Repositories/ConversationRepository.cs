using AiService.Models;
using Dapper;
using Npgsql;

namespace AiService.Repositories
{
    public sealed class ConversationRepository : IConversationRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public ConversationRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<Guid> CreateConversationAsync(CancellationToken ct = default)
        {
            var id = Guid.NewGuid();

            const string sql = """
                INSERT INTO ai_conversations (id, created_on, last_updated_on)
                VALUES (@Id, now(), now());
            """;

            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            await conn.ExecuteAsync(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));

            return id;
        }

        public async Task<bool> ConversationExistsAsync(Guid conversationId, CancellationToken ct = default)
        {
            const string sql = "SELECT 1 FROM ai_conversations WHERE id = @Id;";

            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            var result = await conn.QueryFirstOrDefaultAsync<int?>(
                new CommandDefinition(sql, new { Id = conversationId }, cancellationToken: ct));

            return result.HasValue;
        }

        public async Task AppendMessageAsync(Guid conversationId, string role, string content, CancellationToken ct = default)
        {
            const string insertMsg = """
                INSERT INTO ai_conversation_messages (conversation_id, role, content, created_on)
                VALUES (@ConversationId, @Role, @Content, now());
            """;

            const string touchConv = """
                UPDATE ai_conversations
                SET last_updated_on = now()
                WHERE id = @ConversationId;
            """;

            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            await using var tx = await conn.BeginTransactionAsync(ct);

            await conn.ExecuteAsync(new CommandDefinition(
                insertMsg,
                new { ConversationId = conversationId, Role = role, Content = content },
                transaction: tx,
                cancellationToken: ct));

            await conn.ExecuteAsync(new CommandDefinition(
                touchConv,
                new { ConversationId = conversationId },
                transaction: tx,
                cancellationToken: ct));

            await tx.CommitAsync(ct);
        }

        public async Task<IReadOnlyList<ConversationMessage>> GetRecentMessagesAsync(Guid conversationId, int take, CancellationToken ct = default)
        {
            const string sql = """
                SELECT id, conversation_id AS ConversationId, role, content, created_on AS CreatedOn
                FROM ai_conversation_messages
                WHERE conversation_id = @ConversationId
                ORDER BY created_on DESC
                LIMIT @Take;
            """;

            await using var conn = await _dataSource.OpenConnectionAsync(ct);
            var rows = await conn.QueryAsync<ConversationMessage>(
                new CommandDefinition(sql, new { ConversationId = conversationId, Take = take }, cancellationToken: ct));

            // reverse to chronological order
            return rows.Reverse().ToList();
        }       
    }
}
