using Bank.Domain.Common;
using Bank.Domain.ValueObjects;

namespace Bank.Domain.Accounts;

/// <summary>One side of a transfer in the ledger, with the account's balance right after it.</summary>
public sealed record LedgerPosting(long AccountId, Money Amount, Money BalanceAfter);

/// <summary>
/// Double-entry result of a transfer: the same amount leaves one account and arrives in another, so the
/// postings always cancel out and money is never created or lost.
/// </summary>
public sealed record TransferPostings(LedgerPosting Debit, LedgerPosting Credit);

/// <summary>Business rules for moving money between two accounts held at this bank.</summary>
public static class FundsTransfer
{
    public const int MaxDescriptionLength = 140;

    /// <param name="recipientName">
    /// The name the sender typed for the recipient. As in FAST and EFT, it must match the account holder, which
    /// protects against a mistyped but still valid IBAN that belongs to a stranger.
    /// </param>
    public static TransferPostings Execute(Account source, Account target, Money amount, string recipientName)
    {
        if (source.Id == target.Id)
            throw new DomainException("Gönderen ve alıcı hesap aynı olamaz.");
        if (amount.Round() != amount)
            throw new DomainException("Tutar en fazla iki ondalık basamak içerebilir.");
        if (source.Currency != target.Currency || amount.Currency != source.Currency)
            throw new DomainException("Farklı para birimindeki hesaplar arasında transfer yapılamaz.");
        if (!NamesMatch(recipientName, target.HolderName))
            throw new DomainException("Alıcı adı, IBAN'ın sahibiyle eşleşmiyor.");

        // Both sides are checked before either balance changes, so a refused transfer leaves both accounts untouched.
        source.EnsureCanSend(amount);
        target.EnsureCanReceive(amount);

        source.Withdraw(amount);
        target.Deposit(amount);

        return new TransferPostings(
            new LedgerPosting(source.Id, amount, source.Balance),
            new LedgerPosting(target.Id, amount, target.Balance));
    }

    // "AYŞE  YILMAZ" and "Ayse Yılmaz" are the same person; spacing, case and Turkish letters must not matter.
    private static bool NamesMatch(string typed, string holder)
        => TextFolding.Fold(typed) == TextFolding.Fold(holder);
}
