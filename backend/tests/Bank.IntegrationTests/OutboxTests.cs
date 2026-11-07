using System.Net;
using System.Net.Http.Json;
using Bank.Contracts.Payments;
using Bank.Infrastructure.Messaging;
using Bank.Infrastructure.Persistence;
using Bank.IntegrationTests.Infrastructure;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bank.IntegrationTests;

[Collection(ApiCollection.Name)]
public sealed class OutboxTests
{
    private readonly ApiFactory _factory;

    public OutboxTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Transfer_WritesExactlyOneEvent_EvenWhenRetried()
    {
        var (sender, source, targetIban, _) = await TwoAccountsAsync(100m);

        var transfer = await (await SendAsync(sender, source, targetIban, 40m, "outbox-1")).Content.ReadFromJsonAsync<TransferResponse>();
        await SendAsync(sender, source, targetIban, 40m, "outbox-1");

        var events = await EventsForSourceAsync(source);
        var single = Assert.Single(events);
        Assert.Equal("payments.transfer-completed", single.Type);
        Assert.Contains($"\"transferId\":{transfer!.Id}", single.Payload);
        Assert.Null(single.PublishedAt);
    }

    [Fact]
    public async Task RefusedTransfer_LeavesNoEvent()
    {
        var (sender, source, targetIban, _) = await TwoAccountsAsync(10m);

        var response = await SendAsync(sender, source, targetIban, 50m, "outbox-refused");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await EventsForSourceAsync(source));
    }

    [Fact]
    public async Task Processor_PublishesPendingEventsInOrder_AndMarksThem()
    {
        var (sender, source, targetIban, _) = await TwoAccountsAsync(100m);
        await SendAsync(sender, source, targetIban, 10m, "order-1");
        await SendAsync(sender, source, targetIban, 20m, "order-2");
        var recorder = new RecordingPublisher();

        await DrainAsync(recorder);

        var ours = (await EventsForSourceAsync(source)).Select(e => e.MessageId).ToList();
        Assert.Equal(ours, recorder.Published.Where(m => ours.Contains(m.MessageId)).Select(m => m.MessageId));
        Assert.All(await EventsForSourceAsync(source), e => Assert.NotNull(e.PublishedAt));
    }

    [Fact]
    public async Task BrokerFailure_KeepsTheEventPending_UntilItCanBeSent()
    {
        await DrainAsync(new RecordingPublisher());
        var (sender, source, targetIban, _) = await TwoAccountsAsync(100m);
        await SendAsync(sender, source, targetIban, 10m, "broker-down");

        var published = await Processor(new FailingPublisher()).PublishPendingAsync(50, default);

        Assert.Equal(0, published);
        var pending = Assert.Single(await EventsForSourceAsync(source));
        Assert.Null(pending.PublishedAt);
        Assert.Equal(1, pending.Attempts);
        Assert.Equal("broker unreachable", pending.LastError);

        await DrainAsync(new RecordingPublisher());
        Assert.NotNull(Assert.Single(await EventsForSourceAsync(source)).PublishedAt);
    }

    private async Task<(HttpClient Sender, long Source, string TargetIban, long Target)> TwoAccountsAsync(decimal balance)
    {
        var (sender, senderId) = await _factory.CreateUserAsync();
        var (_, receiverId) = await _factory.CreateUserAsync();
        var (source, _) = await TestData.AddTryAccountAsync(senderId, balance);
        var (target, targetIban) = await TestData.AddTryAccountAsync(receiverId, 0m);
        return (sender, source, targetIban, target);
    }

    private static Task<HttpResponseMessage> SendAsync(HttpClient client, long source, string targetIban, decimal amount, string key)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/transfers")
        {
            Content = JsonContent.Create(new TransferRequest(source, targetIban, "Test Kullanıcı", amount, "Outbox testi"))
        };
        message.Headers.Add("Idempotency-Key", key);
        return client.SendAsync(message);
    }

    private static async Task DrainAsync(IMessagePublisher publisher)
    {
        var processor = Processor(publisher);
        while (await processor.PublishPendingAsync(500, default) > 0)
        {
        }
    }

    private static OutboxProcessor Processor(IMessagePublisher publisher)
        => new(new SqlConnectionFactory(ApiFactory.ConnectionString), publisher, TimeProvider.System, NullLogger<OutboxProcessor>.Instance);

    private static async Task<List<StoredEvent>> EventsForSourceAsync(long sourceAccountId)
    {
        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        var rows = await connection.QueryAsync<StoredEvent>("""
            SELECT MessageId, Type, Payload, PublishedAt, Attempts, LastError
            FROM messaging.OutboxMessages
            WHERE CAST(JSON_VALUE(Payload, '$.sourceAccountId') AS BIGINT) = @SourceAccountId
            ORDER BY Sequence
            """, new { SourceAccountId = sourceAccountId });
        return rows.AsList();
    }

    private sealed class StoredEvent
    {
        public Guid MessageId { get; init; }
        public string Type { get; init; } = string.Empty;
        public string Payload { get; init; } = string.Empty;
        public DateTime? PublishedAt { get; init; }
        public int Attempts { get; init; }
        public string? LastError { get; init; }
    }

    private sealed class RecordingPublisher : IMessagePublisher
    {
        public List<OutboxMessage> Published { get; } = [];

        public Task PublishAsync(OutboxMessage message, CancellationToken ct)
        {
            Published.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FailingPublisher : IMessagePublisher
    {
        public Task PublishAsync(OutboxMessage message, CancellationToken ct)
            => throw new InvalidOperationException("broker unreachable");
    }
}
