using Bank.Domain.Enums;

namespace Bank.Domain.Entities;

/// <summary>
/// DDD Aggregate Root: Banka İşlemi (Transaction)
/// </summary>
public sealed class Transaction
{
    public long Id { get; private set; }
    public string TransactionReference { get; private set; } = Guid.NewGuid().ToString("N");
    public long SenderAccountId { get; private set; }
    public long ReceiverAccountId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; } = TransactionStatus.Completed;
    public string Category { get; private set; } = "General"; // Market, Restoran, Transfer, vb.
    public string Description { get; private set; } = null!;
    public string? IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation Properties
    public ICollection<LedgerEntry> LedgerEntries { get; private set; } = new List<LedgerEntry>();

    private Transaction() { }

    public Transaction(long senderAccountId, long receiverAccountId, decimal amount, string currency, TransactionType type, string category, string description, string? idempotencyKey = null)
    {
        SenderAccountId = senderAccountId;
        ReceiverAccountId = receiverAccountId;
        Amount = amount;
        Currency = currency;
        Type = type;
        Category = category;
        Description = description;
        IdempotencyKey = idempotencyKey;
        Status = TransactionStatus.Completed;
        CreatedAt = DateTime.UtcNow;
    }
}
