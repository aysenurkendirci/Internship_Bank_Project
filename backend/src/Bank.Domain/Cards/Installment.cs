using Bank.Domain.ValueObjects;

namespace Bank.Domain.Cards;

/// <summary>One payment of an installment plan, with the statement it falls on and when it is due.</summary>
public sealed record Installment(int Number, DateOnly StatementDate, DateOnly DueDate, Money Amount);
