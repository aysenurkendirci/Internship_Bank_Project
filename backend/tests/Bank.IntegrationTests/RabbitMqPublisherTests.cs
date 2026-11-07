using System.Text;
using Bank.Infrastructure.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Bank.IntegrationTests;

/// <summary>Runs only where a broker is available (CI starts one); set RABBITMQ_HOST to run it locally.</summary>
public sealed class RabbitMqFactAttribute : FactAttribute
{
    public RabbitMqFactAttribute()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RABBITMQ_HOST")))
            Skip = "RABBITMQ_HOST is not set.";
    }
}

public sealed class RabbitMqPublisherTests
{
    [RabbitMqFact]
    public async Task PublishedMessage_ReachesABoundQueue_WithItsIdAndRoutingKey()
    {
        var options = new MessagingOptions
        {
            RabbitMq = new RabbitMqOptions
            {
                Host = Environment.GetEnvironmentVariable("RABBITMQ_HOST")!,
                Exchange = $"bank.events.test.{Guid.NewGuid():N}"
            }
        };

        await using var publisher = new RabbitMqPublisher(Options.Create(options));
        var message = new OutboxMessage(Guid.NewGuid(), "payments.transfer-completed", """{"transferId":1}""", DateTime.UtcNow);

        // First publish declares the exchange; bind a queue to it, then publish the message we look for.
        await publisher.PublishAsync(message with { MessageId = Guid.NewGuid() }, default);

        var factory = new ConnectionFactory { HostName = options.RabbitMq.Host };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        var queue = (await channel.QueueDeclareAsync(exclusive: true)).QueueName;
        await channel.QueueBindAsync(queue, options.RabbitMq.Exchange, routingKey: "payments.*");

        await publisher.PublishAsync(message, default);

        BasicGetResult? received = null;
        for (var attempt = 0; attempt < 50 && received is null; attempt++)
        {
            received = await channel.BasicGetAsync(queue, autoAck: true);
            if (received is null)
                await Task.Delay(100);
        }

        Assert.NotNull(received);
        Assert.Equal(message.MessageId.ToString(), received.BasicProperties.MessageId);
        Assert.Equal("payments.transfer-completed", received.RoutingKey);
        Assert.Equal(message.Payload, Encoding.UTF8.GetString(received.Body.Span));
    }
}
