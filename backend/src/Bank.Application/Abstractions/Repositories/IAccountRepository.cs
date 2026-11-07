namespace Bank.Application.Abstractions.Repositories;

public interface IAccountRepository
{
    /// <summary>Sum of the user's active account balances in the given currency.</summary>
    Task<decimal> GetAvailableBalanceAsync(long userId, string currencyCode, CancellationToken ct = default);
}
