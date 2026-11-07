using Bank.Application.Common;
using Bank.Domain.Cards;

namespace Bank.Application.Abstractions.Repositories;

/// <summary>
/// Every method is scoped to <c>userId</c>: a card owned by someone else behaves exactly like
/// a card that does not exist.
/// </summary>
public interface ICreditCardRepository
{
    Task<IReadOnlyList<Versioned<CreditCard>>> ListAsync(long userId, CancellationToken ct = default);
    Task<Versioned<CreditCard>?> GetAsync(long userId, long id, CancellationToken ct = default);
    Task<Versioned<CreditCard>> AddAsync(long userId, CreditCard card, CancellationToken ct = default);

    /// <returns>The new version, or <c>null</c> when no live card matched the id and expected version.</returns>
    Task<byte[]?> UpdateAsync(long userId, CreditCard card, byte[] expectedVersion, CancellationToken ct = default);

    /// <returns><c>false</c> when the card does not exist or is still referenced by live items.</returns>
    Task<bool> SoftDeleteIfUnusedAsync(long userId, long id, CancellationToken ct = default);

    Task<bool> ExistsAsync(long userId, long id, CancellationToken ct = default);
    Task<bool> IsInUseAsync(long userId, long id, CancellationToken ct = default);
}
