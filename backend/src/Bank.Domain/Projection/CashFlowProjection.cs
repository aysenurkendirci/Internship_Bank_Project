using Bank.Domain.ValueObjects;

namespace Bank.Domain.Projection;

/// <param name="ExcludedItems">
/// Items left out because they are in another currency than the projection; listed so the
/// result never silently pretends to be complete.
/// </param>
public sealed record CashFlowProjection(
    DateOnly From,
    DateOnly To,
    Money OpeningBalance,
    IReadOnlyList<ProjectionEntry> Entries,
    IReadOnlyList<MonthlyCashFlow> Months,
    IReadOnlyList<string> ExcludedItems);
