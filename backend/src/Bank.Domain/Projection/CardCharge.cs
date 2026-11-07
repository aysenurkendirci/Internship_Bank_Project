using Bank.Domain.ValueObjects;

namespace Bank.Domain.Projection;

/// <summary>One line on a credit card statement: an installment or a card-paid recurring item.</summary>
public sealed record CardCharge(string Description, Money Amount);
