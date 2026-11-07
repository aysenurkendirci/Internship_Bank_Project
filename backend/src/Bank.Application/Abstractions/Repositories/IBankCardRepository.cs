using Bank.Domain.Cards;

namespace Bank.Application.Abstractions.Repositories;

public interface IBankCardRepository
{
    /// <summary>The card, if it belongs to the user.</summary>
    Task<BankCard?> GetAsync(long userId, long cardId, CancellationToken ct = default);

    Task UpdateAsync(BankCard card, CancellationToken ct = default);
}
