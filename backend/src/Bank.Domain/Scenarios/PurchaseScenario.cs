namespace Bank.Domain.Scenarios;

/// <summary>A planned card purchase and the payment options to compare. Nothing here is persisted.</summary>
public sealed record PurchaseScenario(
    long CreditCardId,
    string Description,
    DateOnly PurchaseDate,
    IReadOnlyList<PurchaseOption> Options);
