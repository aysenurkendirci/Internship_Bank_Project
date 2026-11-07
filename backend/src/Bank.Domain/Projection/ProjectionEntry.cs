using Bank.Domain.ValueObjects;

namespace Bank.Domain.Projection;

/// <summary>
/// A dated movement on the account. <see cref="Amount"/> is signed: income positive, outflows negative.
/// For <see cref="ProjectionEntryKind.CardPayment"/> entries, <see cref="Charges"/> lists what the
/// statement is made of; for all other kinds it is empty.
/// </summary>
public sealed record ProjectionEntry(
    DateOnly Date,
    string Description,
    ProjectionEntryKind Kind,
    Money Amount,
    long? CreditCardId,
    DateOnly? StatementDate,
    IReadOnlyList<CardCharge> Charges);
