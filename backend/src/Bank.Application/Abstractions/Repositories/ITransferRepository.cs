using Bank.Domain.Accounts;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Abstractions.Repositories;

/// <remarks>
/// The locking methods only make sense inside a unit of work: the locks they take are held until the
/// transaction ends, which is what keeps two concurrent transfers from spending the same balance.
/// </remarks>
public interface ITransferRepository
{
    /// <summary>
    /// Finds an earlier transfer with the same idempotency key and locks the key, so a concurrent retry with the
    /// same key waits for this one instead of running in parallel.
    /// </summary>
    Task<StoredTransfer?> FindByIdempotencyKeyAsync(long userId, string idempotencyKey, CancellationToken ct = default);

    Task<long?> FindAccountIdAsync(Iban iban, CancellationToken ct = default);

    /// <summary>Loads and locks the accounts, always in ascending id order so two opposite transfers cannot deadlock.</summary>
    Task<IReadOnlyList<Account>> LockAccountsAsync(IReadOnlyCollection<long> accountIds, CancellationToken ct = default);

    Task<StoredTransfer> AddAsync(NewTransfer transfer, CancellationToken ct = default);

    /// <summary>A transfer sent by the user.</summary>
    Task<StoredTransfer?> GetAsync(long userId, long transferId, CancellationToken ct = default);
}

public sealed record NewTransfer(
    long UserId,
    string IdempotencyKey,
    byte[] RequestHash,
    Account Source,
    Account Target,
    Money Amount,
    string Description,
    TransferPostings Postings);

public sealed class StoredTransfer
{
    public long TransferId { get; init; }
    public byte[] RequestHash { get; init; } = [];
    public long SourceAccountId { get; init; }
    public string SourceIban { get; init; } = string.Empty;
    public string TargetIban { get; init; } = string.Empty;
    public string RecipientName { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal SourceBalanceAfter { get; init; }
    public DateTime CreatedAt { get; init; }
}
