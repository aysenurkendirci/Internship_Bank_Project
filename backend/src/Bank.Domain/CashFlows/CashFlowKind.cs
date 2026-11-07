namespace Bank.Domain.CashFlows;

public enum CashFlowKind
{
    Income,
    Expense,

    /// <summary>An expense the user can cancel; tracked separately so it can be reviewed as a group.</summary>
    Subscription
}
