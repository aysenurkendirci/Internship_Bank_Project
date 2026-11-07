using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Bank.Infrastructure.Messaging;

/// <summary>
/// Publishes outbox messages to a durable topic exchange with publisher confirms: a message counts as sent only
/// after the broker has confirmed it, otherwise <see cref="PublishAsync"/> throws and the row stays pending.
/// </summary>
public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(IOptions<MessagingOptions> options) => _options = options.Value.RabbitMq;

    public async Task PublishAsync(OutboxMessage message, CancellationToken ct)
    {
        var channel = await GetChannelAsync(ct);

        var properties = new BasicProperties
        {
            // Consumers deduplicate on this id; delivery is at least once.
            MessageId = message.MessageId.ToString(),
            Type = message.Type,
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Timestamp = new AmqpTimestamp(new DateTimeOffset(message.OccurredAt, TimeSpan.Zero).ToUnixTimeSeconds())
        };

        await channel.BasicPublishAsync(
            _options.Exchange,
            routingKey: message.Type,
            mandatory: false,
            properties,
            Encoding.UTF8.GetBytes(message.Payload),
            ct);
    }

    // One connection and channel for the life of the app, reopened if the broker dropped them.
    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _gate.WaitAsync(ct);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            await DisposeConnectionAsync();

            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                Port = _options.Port,
                VirtualHost = _options.VirtualHost,
                UserName = _options.UserName,
                Password = _options.Password,
                ClientProvidedName = "bank-api-outbox"
            };

            _connection = await factory.CreateConnectionAsync(ct);
            _channel = await _connection.CreateChannelAsync(
                new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
                ct);
            await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, durable: true, autoDelete: false, cancellationToken: ct);

            return _channel;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task DisposeConnectionAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();

        _channel = null;
        _connection = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
        _gate.Dispose();
    }
}
