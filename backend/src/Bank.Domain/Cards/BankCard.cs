using Bank.Domain.Common;

namespace Bank.Domain.Cards;

public enum BankCardStatus
{
    Active,
    Blocked,
    Cancelled
}

/// <summary>
/// A debit or credit card issued by the bank on one of the customer's accounts. Only the settings the customer
/// controls live here; the card number itself is never held by this system (PCI DSS), only its last four digits.
/// </summary>
public sealed class BankCard
{
    public long Id { get; }
    public long UserId { get; }
    public BankCardStatus Status { get; private set; }
    public bool Contactless { get; private set; }
    public bool OnlineUse { get; private set; }
    public decimal DailyLimit { get; private set; }
    public decimal MonthlyLimit { get; private set; }

    public BankCard(long id, long userId, BankCardStatus status, bool contactless, bool onlineUse, decimal dailyLimit, decimal monthlyLimit)
    {
        Id = id;
        UserId = userId;
        Status = status;
        Contactless = contactless;
        OnlineUse = onlineUse;
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
    }

    public void ChangeSettings(bool contactless, bool onlineUse, decimal dailyLimit, decimal monthlyLimit)
    {
        EnsureNotCancelled();

        if (dailyLimit < 0)
            throw new DomainException("Günlük limit negatif olamaz.");
        if (monthlyLimit < dailyLimit)
            throw new DomainException("Aylık limit günlük limitten küçük olamaz.");

        Contactless = contactless;
        OnlineUse = onlineUse;
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
    }

    /// <summary>Temporarily stops the card, e.g. when the customer cannot find it; it can be reopened.</summary>
    public void Block()
    {
        EnsureNotCancelled();
        Status = BankCardStatus.Blocked;
    }

    public void Unblock()
    {
        EnsureNotCancelled();
        Status = BankCardStatus.Active;
    }

    // Cancellation is final: a cancelled card is replaced by a new one, never revived.
    private void EnsureNotCancelled()
    {
        if (Status == BankCardStatus.Cancelled)
            throw new DomainException("İptal edilmiş kart üzerinde işlem yapılamaz.");
    }
}
