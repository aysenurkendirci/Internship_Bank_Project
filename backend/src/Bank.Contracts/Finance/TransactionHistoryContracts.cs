namespace Bank.Contracts.Finance;

public sealed record ImportResultResponse(Guid BatchId, int TotalRows, int ImportedRows, int DuplicateRows);

/// <param name="Amount">Signed: money in positive, money out negative.</param>
public sealed record HistoryTransactionResponse(
    long Id, DateOnly Date, string Description, decimal Amount, string CurrencyCode, string? Category);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
