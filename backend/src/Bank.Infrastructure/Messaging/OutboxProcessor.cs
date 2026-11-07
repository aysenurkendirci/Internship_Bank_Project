using Bank.Infrastructure.Persistence;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Bank.Infrastructure.Messaging;

public sealed record OutboxMessage(Guid MessageId, string Type, string Payload, DateTime OccurredAt);

/// <summary>Sends a stored event to the message broker.</summary>
public interface IMessagePublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken ct);
}

/// <summary>Forwards pending outbox rows to the broker, oldest first.</summary>
public sealed class OutboxProcessor
{
    private const int MaxErrorLength = 1000;

    private readonly SqlConnectionFactory _connections;
    private readonly IMessagePublisher _publisher;
    private readonly TimeProvider _time;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(SqlConnectionFactory connections, IMessagePublisher publisher, TimeProvider time, ILogger<OutboxProcessor> logger)
    {
        _connections = connections;
        _publisher = publisher;
        _time = time;
        _logger = logger;
    }

    /// <returns>How many messages were published.</returns>
    /// <remarks>
    /// Rows are claimed with UPDLOCK + READPAST, so two publisher instances share the work instead of sending the
    /// same row twice. A failed message stops the batch: sending later events before it would break their order.
    /// </remarks>
    public async Task<int> PublishPendingAsync(int batchSize, CancellationToken ct)
    {
        const string claim = """
            SELECT TOP (@BatchSize) MessageId, Type, Payload, OccurredAt
            FROM messaging.OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
            WHERE PublishedAt IS NULL
            ORDER BY Sequence
            """;

        await using var connection = await _connections.OpenAsync(ct);
        await using var transaction = connection.BeginTransaction();

        var messages = (await connection.QueryAsync<OutboxMessage>(
            new CommandDefinition(claim, new { BatchSize = batchSize }, transaction, cancellationToken: ct))).AsList();

        var published = 0;
        foreach (var message in messages)
        {
            try
            {
                await _publisher.PublishAsync(message, ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                _logger.LogWarning(e, "Publishing outbox message {MessageId} ({Type}) failed", message.MessageId, message.Type);
                await connection.ExecuteAsync(new CommandDefinition(
                    "UPDATE messaging.OutboxMessages SET Attempts = Attempts + 1, LastError = @Error WHERE MessageId = @MessageId",
                    new { message.MessageId, Error = Truncate(e.Message) }, transaction, cancellationToken: ct));
                break;
            }

            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE messaging.OutboxMessages SET PublishedAt = @Now, Attempts = Attempts + 1 WHERE MessageId = @MessageId",
                new { message.MessageId, Now = _time.GetUtcNow().UtcDateTime }, transaction, cancellationToken: ct));
            published++;
        }

        await transaction.CommitAsync(ct);
        return published;
    }

    private static string Truncate(string text) => text.Length <= MaxErrorLength ? text : text[..MaxErrorLength];
}
