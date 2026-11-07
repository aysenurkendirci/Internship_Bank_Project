using Bank.Domain.ValueObjects;

namespace Bank.Domain.Scenarios;

/// <summary>
/// One way of paying for the purchase. Each option carries its own total because merchants often
/// charge more for longer installment plans; assuming the same price would flatter those options.
/// </summary>
public sealed record PurchaseOption(int InstallmentCount, Money TotalAmount);
