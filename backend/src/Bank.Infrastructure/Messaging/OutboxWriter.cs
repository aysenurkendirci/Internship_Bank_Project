using System.Text.Json;
using Bank.Application.Abstractions.Messaging;
using Bank.Contracts.Events;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Messaging;

public sealed class OutboxWriter : IOutbox
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly DbSession _session;
    private readonly TimeProvider _time;

    public OutboxWriter(DbSession session, TimeProvider time)
    {
        _session = session;
        _time = time;
    }

    public async Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        // Outside a transaction the event could outlive a rolled-back change, which is exactly what the outbox prevents.
        var transaction = _session.Transaction
                          ?? throw new InvalidOperationException("Outbox events must be added inside a unit of work.");

        const string sql = """
            INSERT INTO messaging.OutboxMessages (MessageId, Type, Payload, OccurredAt)
            VALUES (@MessageId, @Type, @Payload, @OccurredAt)
            """;

        var connection = await _session.GetConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            MessageId = Guid.NewGuid(),
            Type = integrationEvent.EventType,
            Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), Json),
            OccurredAt = _time.GetUtcNow().UtcDateTime
        }, transaction, cancellationToken: ct));
    }
}
