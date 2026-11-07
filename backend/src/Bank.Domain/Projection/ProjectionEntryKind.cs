namespace Bank.Domain.Projection;

public enum ProjectionEntryKind
{
    Income,
    Expense,
    Subscription,

    /// <summary>A credit card statement settled from the account on its due date.</summary>
    CardPayment
}
