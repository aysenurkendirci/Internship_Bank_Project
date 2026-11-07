namespace Bank.Contracts.Finance;

/// <param name="ExcludedItems">Items in another currency that are not part of the projection.</param>
public sealed record ProjectionResponse(
    DateOnly From,
    DateOnly To,
    string CurrencyCode,
    decimal OpeningBalance,
    IReadOnlyList<ProjectionMonthResponse> Months,
    IReadOnlyList<ProjectionEntryResponse> Entries,
    IReadOnlyList<string> ExcludedItems
);

public sealed record ProjectionMonthResponse(
    int Year,
    int Month,
    decimal Income,
    decimal Outflows,
    decimal CardPayments,
    decimal Net,
    decimal ClosingBalance,
    decimal LowestBalance,
    DateOnly? LowestBalanceDate,
    decimal? BurdenRatio,
    bool IsDeficit,
    bool BalanceGoesNegative
);

/// <param name="Kind">Income, Expense, Subscription or CardPayment.</param>
/// <param name="Amount">Signed: income positive, outflows negative.</param>
public sealed record ProjectionEntryResponse(
    DateOnly Date,
    string Description,
    string Kind,
    decimal Amount,
    long? CreditCardId,
    DateOnly? StatementDate,
    IReadOnlyList<CardChargeResponse> Charges
);

public sealed record CardChargeResponse(string Description, decimal Amount);
