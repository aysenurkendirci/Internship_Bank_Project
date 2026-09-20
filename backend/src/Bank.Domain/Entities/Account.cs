using Bank.Domain.Enums;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Entities;

/// <summary>
/// DDD Entity: Banka Hesabı
/// Bakiye hareketleri doğrudan property üzerinden değiştirilemez. 
/// Deposit(), Withdraw() gibi iş metotları (Domain Methods) üzerinden kontrol edilir.
/// </summary>
public sealed class Account
{
    public long Id { get; private set; }
    public long UserId { get; private set; }
    public string AccountNumber { get; private set; } = null!;
    public Iban Iban { get; private set; } = null!;
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public AccountType Type { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Optimistic Concurrency Control için Concurrency Token
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // Navigation Properties
    public User User { get; private set; } = null!;
    public ICollection<Card> Cards { get; private set; } = new List<Card>();
    public ICollection<LedgerEntry> LedgerEntries { get; private set; } = new List<LedgerEntry>();

    private Account() { } // EF Core ctor

    public Account(long userId, string accountNumber, string iban, AccountType type, string currency = "TRY")
    {
        UserId = userId;
        AccountNumber = accountNumber;
        Iban = ValueObjects.Iban.Create(iban);
        Type = type;
        Currency = currency.ToUpperInvariant();
        Balance = 0m;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Money GetMoney() => Money.Create(Balance, Currency);

    public void Deposit(Money amount)
    {
        EnsureActive();
        if (amount.Amount <= 0)
            throw new DomainException("Yatırılacak tutar 0'dan büyük olmalıdır.");

        if (amount.Currency != Currency)
            throw new CurrencyMismatchException(Currency, amount.Currency);

        Balance += amount.Amount;
    }

    public void Withdraw(Money amount)
    {
        EnsureActive();
        if (amount.Amount <= 0)
            throw new DomainException("Çekilecek tutar 0'dan büyük olmalıdır.");

        if (amount.Currency != Currency)
            throw new CurrencyMismatchException(Currency, amount.Currency);

        if (Balance < amount.Amount)
            throw new InsufficientFundsException(amount.Amount, Balance);

        Balance -= amount.Amount;
    }

    public void Lock()
    {
        IsActive = false;
    }

    public void Unlock()
    {
        IsActive = true;
    }

    private void EnsureActive()
    {
        if (!IsActive)
            throw new AccountLockedException(Id);
    }
}
