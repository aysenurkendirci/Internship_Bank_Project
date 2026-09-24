using Bank.Domain.Entities;

namespace Bank.Application.Abstractions.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdWithUserAsync(long id, CancellationToken cancellationToken = default);
    Task AddAsync(Account account, CancellationToken cancellationToken = default);
    Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);
}
