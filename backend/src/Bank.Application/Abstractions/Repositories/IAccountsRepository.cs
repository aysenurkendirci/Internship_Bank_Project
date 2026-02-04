using Bank.Contracts.Accounts;

namespace Bank.Application.Abstractions.Repositories;

public interface IAccountsRepository
{
    Task<AccountDetailResponse?> GetAccountDetailAsync(long accountId, CancellationToken ct = default);
}
