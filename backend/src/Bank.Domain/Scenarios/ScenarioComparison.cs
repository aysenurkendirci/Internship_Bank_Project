using Bank.Domain.Cards;

namespace Bank.Domain.Scenarios;

public sealed record OptionOutcome(PurchaseOption Option, IReadOnlyList<Installment> Installments, ScenarioMetrics Metrics);

/// <param name="Baseline">The same horizon without the purchase, as a reference point.</param>
public sealed record ScenarioComparison(
    DateOnly From,
    DateOnly To,
    ScenarioMetrics Baseline,
    IReadOnlyList<OptionOutcome> Options);
