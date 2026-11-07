using System.Text.Json.Serialization;

namespace Bank.Contracts.Events;

/// <summary>An event other systems can subscribe to. Its shape is a public contract: add fields, never rename them.</summary>
public interface IIntegrationEvent
{
    /// <summary>Also the routing key on the broker, e.g. <c>payments.transfer-completed</c>.</summary>
    [JsonIgnore]
    string EventType { get; }
}

/// <summary>
/// Raised once a transfer is committed, e.g. for notifications or fraud monitoring. Carries ids and amounts only:
/// consumers that need names or IBANs ask for them, so personal data is not copied into every queue.
/// </summary>
public sealed record TransferCompleted(
    long TransferId,
    long SourceAccountId,
    long TargetAccountId,
    decimal Amount,
    string CurrencyCode,
    DateTime OccurredAt) : IIntegrationEvent
{
    [JsonIgnore]
    public string EventType => "payments.transfer-completed";
}
