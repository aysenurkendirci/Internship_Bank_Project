using Bank.Contracts.Cards;

namespace Bank.Application.Abstractions.Repositories;

public interface ICardsRepository
{
    Task<CardDetailResponse?> GetCardDetailAsync(long cardId, CancellationToken ct = default);
}
