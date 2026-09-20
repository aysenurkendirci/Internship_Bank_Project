using Bank.Domain.Enums;

namespace Bank.Domain.Entities;

/// <summary>
/// DDD Entity: Muhasebe Defter-i Kebir Kaydı (Double-Entry Bookkeeping Ledger Entry)
/// Kurumsal bankacılıkta her mali işlem çift taraflı (Borç / Alacak) defter kaydı oluşturur.
/// Bu kayıtlar DEĞİŞTİRİLEMEZ (Immutable Audit Log).
/// </summary>
public sealed class LedgerEntry
{
    public long Id { get; private set; }
    public long TransactionId { get; private set; }
    public long AccountId { get; private set; }
    public LedgerEntryType EntryType { get; private set; } // Debit (Borç) veya Credit (Alacak)
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public string Description { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation Properties
    public Account Account { get; private set; } = null!;
    public Transaction Transaction { get; private set; } = null!;

    private LedgerEntry() { }

    public LedgerEntry(long transactionId, long accountId, LedgerEntryType entryType, decimal amount, string currency, string description)
    {
        TransactionId = transactionId;
        AccountId = accountId;
        EntryType = entryType;
        Amount = amount;
        Currency = currency;
        Description = description;
        CreatedAt = DateTime.UtcNow;
    }
}
