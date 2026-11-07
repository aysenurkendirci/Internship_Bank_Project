using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Accounts;

public enum AccountStatus
{
    Active,
    Frozen,
    Closed
}

/// <summary>A customer's bank account. The balance only changes through <see cref="Withdraw"/> and <see cref="Deposit"/>.</summary>
public sealed class Account
{
    public long Id { get; }
    public long UserId { get; }
    public Iban Iban { get; }
    public string HolderName { get; }
    public AccountStatus Status { get; }
    public Money Balance { get; private set; }

    public Currency Currency => Balance.Currency;

    public Account(long id, long userId, Iban iban, string holderName, AccountStatus status, Money balance)
    {
        ArgumentNullException.ThrowIfNull(iban);
        ArgumentException.ThrowIfNullOrWhiteSpace(holderName);
        ArgumentNullException.ThrowIfNull(balance);

        Id = id;
        UserId = userId;
        Iban = iban;
        HolderName = holderName;
        Status = status;
        Balance = balance;
    }

    public void Withdraw(Money amount)
    {
        EnsureCanSend(amount);
        Balance -= amount;
    }

    public void Deposit(Money amount)
    {
        EnsureCanReceive(amount);
        Balance += amount;
    }

    public void EnsureCanSend(Money amount)
    {
        EnsureUsable(amount, "Gönderen hesap işleme kapalı.");

        if (amount > Balance)
            throw new DomainException("Hesap bakiyesi yetersiz.");
    }

    public void EnsureCanReceive(Money amount) => EnsureUsable(amount, "Alıcı hesap işleme kapalı.");

    // A frozen account (e.g. under a legal hold) can neither send nor receive; a closed one no longer exists for transfers.
    private void EnsureUsable(Money amount, string closedMessage)
    {
        if (Status != AccountStatus.Active)
            throw new DomainException(closedMessage);
        if (!amount.IsPositive)
            throw new DomainException("Tutar sıfırdan büyük olmalıdır.");
    }
}
