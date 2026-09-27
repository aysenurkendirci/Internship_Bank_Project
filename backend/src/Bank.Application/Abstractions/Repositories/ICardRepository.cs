using Bank.Domain.Entities;

namespace Bank.Application.Abstractions.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByIdWithAccountAsync(long cardId, CancellationToken cancellationToken = default);
    Task UpdateAsync(Card card, CancellationToken cancellationToken = default);
}
