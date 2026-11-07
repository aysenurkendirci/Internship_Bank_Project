using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Cards;

/// <summary>
/// A card purchase split into equal monthly installments. The first installment is billed on the
/// statement the purchase falls on and each following one on the next statement.
/// </summary>
public sealed class InstallmentPlan
{
    /// <summary>
    /// Sanity bound only. Regulators cap installments per merchant category; those caps are not modelled.
    /// </summary>
    public const int MaxInstallmentCount = 36;

    public long Id { get; }
    public long CreditCardId { get; }
    public string Description { get; }
    public Money TotalAmount { get; }
    public int InstallmentCount { get; }
    public DateOnly PurchaseDate { get; }

    public InstallmentPlan(
        long id,
        long creditCardId,
        string description,
        Money totalAmount,
        int installmentCount,
        DateOnly purchaseDate)
    {
        ArgumentNullException.ThrowIfNull(totalAmount);

        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Açıklama boş olamaz.");
        if (!totalAmount.IsPositive)
            throw new DomainException("Tutar sıfırdan büyük olmalıdır.");
        if (installmentCount is < 1 or > MaxInstallmentCount)
            throw new DomainException($"Taksit sayısı 1 ile {MaxInstallmentCount} arasında olmalıdır.");

        Id = id;
        CreditCardId = creditCardId;
        Description = description.Trim();
        TotalAmount = totalAmount;
        InstallmentCount = installmentCount;
        PurchaseDate = purchaseDate;
    }

    /// <summary>
    /// Builds the payment schedule on <paramref name="card"/>'s billing cycle. Installment amounts
    /// always add up to the purchase total; any rounding remainder sits on the first installment.
    /// </summary>
    public IReadOnlyList<Installment> ScheduleOn(CreditCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        if (card.Id != CreditCardId)
            throw new DomainException("Taksit planı başka bir karta ait.");
        if (card.Limit.Currency != TotalAmount.Currency)
            throw new DomainException("Taksit tutarı kartın para biriminde olmalıdır.");

        var amounts = TotalAmount.Allocate(InstallmentCount);
        var schedule = new Installment[InstallmentCount];
        var statementDate = card.StatementDateFor(PurchaseDate);

        for (var i = 0; i < InstallmentCount; i++)
        {
            schedule[i] = new Installment(i + 1, statementDate, card.DueDateFor(statementDate), amounts[i]);
            statementDate = card.NextStatementDate(statementDate);
        }

        return schedule;
    }
}
