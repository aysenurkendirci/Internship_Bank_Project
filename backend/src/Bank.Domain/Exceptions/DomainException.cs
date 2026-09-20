namespace Bank.Domain.Exceptions;

/// <summary>
/// Domain katmanındaki iş kuralları ihlal edildiğinde fırlatılan temel exception sınıfı.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}

public sealed class InsufficientFundsException : DomainException
{
    public InsufficientFundsException(decimal requestedAmount, decimal currentBalance) 
        : base($"Yetersiz bakiye. İstenen Tutar: {requestedAmount:N2}, Mevcut Bakiye: {currentBalance:N2}")
    {
    }
}

public sealed class CurrencyMismatchException : DomainException
{
    public CurrencyMismatchException(string sourceCurrency, string targetCurrency)
        : base($"Farklı para birimleri arasında doğrudan işlem yapılamaz: {sourceCurrency} != {targetCurrency}")
    {
    }
}

public sealed class InvalidTcNoException : DomainException
{
    public InvalidTcNoException(string tcNo) : base($"Geçersiz TC Kimlik Numarası formatı: {tcNo}")
    {
    }
}

public sealed class InvalidIbanException : DomainException
{
    public InvalidIbanException(string iban) : base($"Geçersiz IBAN formatı veya kontrol hanesi: {iban}")
    {
    }
}

public sealed class AccountLockedException : DomainException
{
    public AccountLockedException(long accountId) : base($"Hesap geçici olarak kilitli durumdadır. Hesap ID: {accountId}")
    {
    }
}
