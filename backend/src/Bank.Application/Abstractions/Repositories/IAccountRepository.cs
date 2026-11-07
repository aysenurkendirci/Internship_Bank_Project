using Bank.Domain.Accounts;

namespace Bank.Application.Abstractions.Repositories;

public interface IAccountRepository
{
    /// <summary>Sum of the user's active account balances in the given currency.</summary>
    Task<decimal> GetAvailableBalanceAsync(long userId, string currencyCode, CancellationToken ct = default);

    Task<IReadOnlyList<AccountRow>> ListAsync(long userId, CancellationToken ct = default);

    /// <summary>The next free account number; the IBAN of a new account is built from it.</summary>
    Task<long> NextAccountNumberAsync(CancellationToken ct = default);

    Task<AccountRow> OpenAsync(long userId, string accountType, string currencyCode, Iban iban, CancellationToken ct = default);

    Task<bool> BelongsToAsync(long userId, long accountId, CancellationToken ct = default);

    /// <summary>Ledger lines of one account, newest first.</summary>
    Task<(IReadOnlyList<StatementEntryRow> Items, int TotalCount)> ListStatementAsync(
        long accountId, int page, int pageSize, CancellationToken ct = default);
}

public sealed class AccountRow
{
    public long AccountId { get; init; }
    public string Iban { get; init; } = string.Empty;
    public string AccountType { get; init; } = string.Empty;
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class StatementEntryRow
{
    public long EntryId { get; init; }
    public long TransferId { get; init; }
    public string Side { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public decimal BalanceAfter { get; init; }
    public string Description { get; init; } = string.Empty;
    public string CounterpartyIban { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
