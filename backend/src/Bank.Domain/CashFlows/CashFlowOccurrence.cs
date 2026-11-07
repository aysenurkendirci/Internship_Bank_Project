using Bank.Domain.ValueObjects;

namespace Bank.Domain.CashFlows;

/// <summary>
/// A single dated instance of a recurring cash flow.
/// <see cref="NetAmount"/> is signed from the user's point of view: income is positive, outflows negative.
/// </summary>
public sealed record CashFlowOccurrence(DateOnly Date, string Name, CashFlowKind Kind, Money NetAmount);
