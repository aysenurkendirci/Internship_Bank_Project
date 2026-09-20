using Bank.Domain.Enums;

namespace Bank.Domain.Entities;

/// <summary>
/// DDD Entity: Kart (Banka Kartı / Sanal Kart)
/// </summary>
public sealed class Card
{
    public long Id { get; private set; }
    public long AccountId { get; private set; }
    public string CardNumber { get; private set; } = null!;
    public string CardHolderName { get; private set; } = null!;
    public string ExpiryDate { get; private set; } = null!; // MM/YY
    public string Cvv { get; private set; } = null!;
    public CardType Type { get; private set; }
    public bool IsVirtual { get; private set; }
    public bool IsContactlessEnabled { get; private set; } = true;
    public bool IsOnlineEnabled { get; private set; } = true;
    public decimal DailyLimit { get; private set; } = 5000m;
    public decimal MonthlyLimit { get; private set; } = 50000m;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation Property
    public Account Account { get; private set; } = null!;

    private Card() { }

    public Card(long accountId, string cardNumber, string cardHolderName, string expiryDate, string cvv, CardType type, bool isVirtual)
    {
        AccountId = accountId;
        CardNumber = cardNumber;
        CardHolderName = cardHolderName;
        ExpiryDate = expiryDate;
        Cvv = cvv;
        Type = type;
        IsVirtual = isVirtual;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLimits(decimal dailyLimit, decimal monthlyLimit)
    {
        DailyLimit = dailyLimit;
        MonthlyLimit = monthlyLimit;
    }

    public void ToggleContactless(bool enable) => IsContactlessEnabled = enable;
    public void ToggleOnline(bool enable) => IsOnlineEnabled = enable;
}
