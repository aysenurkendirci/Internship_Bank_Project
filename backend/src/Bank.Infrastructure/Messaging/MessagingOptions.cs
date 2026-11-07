namespace Bank.Infrastructure.Messaging;

/// <summary>Bound from the <c>Messaging</c> configuration section.</summary>
public sealed class MessagingOptions
{
    public const string Section = "Messaging";

    /// <summary>
    /// Off by default: without a broker, events simply wait in the outbox and are sent once publishing is turned on.
    /// </summary>
    public bool Enabled { get; init; }

    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(2);

    public int BatchSize { get; init; } = 50;

    public RabbitMqOptions RabbitMq { get; init; } = new();
}

public sealed class RabbitMqOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    public string VirtualHost { get; init; } = "/";
    public string UserName { get; init; } = "guest";
    public string Password { get; init; } = "guest";

    /// <summary>A topic exchange: consumers bind with patterns such as <c>payments.*</c>.</summary>
    public string Exchange { get; init; } = "bank.events";
}
