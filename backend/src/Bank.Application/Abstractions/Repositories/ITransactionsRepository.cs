using Bank.Contracts.Transactions;

namespace Bank.Application.Abstractions.Repositories;

public interface ITransactionsRepository
{
    Task<IReadOnlyList<TransactionItem>> GetRecentAsync(
        long userId,
        long? accountId,
        long? cardId,
        int take,
        CancellationToken ct);
}
