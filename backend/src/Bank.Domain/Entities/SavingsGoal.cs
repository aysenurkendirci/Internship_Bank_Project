namespace Bank.Domain.Entities;

/// <summary>
/// DDD Entity: FinGen Hedef Birikim Kumbarası (Savings Goal / Piggy Bank)
/// </summary>
public sealed class SavingsGoal
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string Title { get; private set; } = null!;
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public string IconName { get; private set; } = "target";
    public DateTime TargetDate { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation Property
    public User User { get; private set; } = null!;
    private SavingsGoal() { }

    public SavingsGoal(long userId, string title, decimal targetAmount, DateTime targetDate, string iconName = "target", string currency = "TRY")
    {
        UserId = userId;
        Title = title;
        TargetAmount = targetAmount;
        CurrentAmount = 0m;
        TargetDate = targetDate;
        IconName = iconName;
        Currency = currency;
        IsCompleted = false;
        CreatedAt = DateTime.UtcNow;
    }

    public void AddFunds(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Eklenecek tutar 0'dan büyük olmalıdır.");

        CurrentAmount += amount;
        if (CurrentAmount >= TargetAmount)
        {
            IsCompleted = true;
        }
    }
}
