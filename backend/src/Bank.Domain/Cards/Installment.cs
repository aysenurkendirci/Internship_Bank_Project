using Bank.Domain.ValueObjects;

namespace Bank.Domain.Cards;

public sealed record Installment(int Number, DateOnly StatementDate, DateOnly DueDate, Money Amount);
