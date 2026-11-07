namespace Bank.Application.Abstractions.Repositories;

/// <param name="Amount">Signed: money in positive, money out negative.</param>
public sealed record HistoryTransaction(
    DateOnly Date, string Description, decimal Amount, string CurrencyCode, string? Category, byte[] Fingerprint);

public sealed record StoredHistoryTransaction(
    long Id, DateOnly Date, string Description, decimal Amount, string CurrencyCode, string? Category);

public sealed record ImportOutcome(Guid BatchId, int TotalRows, int ImportedRows, int DuplicateRows);

public interface ITransactionHistoryRepository
{
    /// <summary>
    /// Stores every transaction whose fingerprint the user does not already have and records the
    /// batch. Runs as one transaction: either the whole file is processed or nothing is stored.
    /// </summary>
    Task<ImportOutcome> ImportAsync(
        long userId, string source, string? fileName, IReadOnlyList<HistoryTransaction> transactions,
        CancellationToken ct = default);

    Task<(IReadOnlyList<StoredHistoryTransaction> Items, int TotalCount)> ListAsync(
        long userId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default);
}
