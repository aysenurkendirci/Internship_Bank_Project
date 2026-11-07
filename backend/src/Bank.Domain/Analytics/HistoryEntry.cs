namespace Bank.Domain.Analytics;

/// <summary>
/// One past transaction as the analytics see it. Amounts are in the history's single currency (TRY)
/// and signed: money in positive, money out negative.
/// </summary>
public sealed record HistoryEntry(DateOnly Date, string Description, decimal Amount, string? Category)
{
    public const string Uncategorised = "Kategorisiz";

    public bool IsOutflow => Amount < 0;

    public string CategoryOrDefault => string.IsNullOrWhiteSpace(Category) ? Uncategorised : Category;
}
