using Bank.Application.Common;
using Bank.Domain.CashFlows;

namespace Bank.Application.Abstractions.Repositories;

/// <summary>Every method is scoped to <c>userId</c>; see <see cref="ICreditCardRepository"/>.</summary>
public interface IRecurringCashFlowRepository
{
    Task<IReadOnlyList<Versioned<RecurringCashFlow>>> ListAsync(long userId, CashFlowKind? kind, CancellationToken ct = default);
    Task<Versioned<RecurringCashFlow>?> GetAsync(long userId, long id, CancellationToken ct = default);
    Task<Versioned<RecurringCashFlow>> AddAsync(long userId, RecurringCashFlow item, CancellationToken ct = default);

    /// <returns>The new version, or <c>null</c> when no live item matched the id and expected version.</returns>
    Task<byte[]?> UpdateAsync(long userId, RecurringCashFlow item, byte[] expectedVersion, CancellationToken ct = default);

    Task<bool> SoftDeleteAsync(long userId, long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(long userId, long id, CancellationToken ct = default);
}
