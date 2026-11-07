using Bank.Contracts.Events;

namespace Bank.Application.Abstractions.Messaging;

/// <summary>
/// Records an event in the current unit of work. It is published only if the transaction commits, so an event
/// never describes a change that was rolled back.
/// </summary>
public interface IOutbox
{
    Task AddAsync(IIntegrationEvent integrationEvent, CancellationToken ct = default);
}
