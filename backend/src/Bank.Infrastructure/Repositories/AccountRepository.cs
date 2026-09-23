using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Entities;
using Bank.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bank.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly BankDbContext _context;

    public AccountRepository(BankDbContext context)
    {
        _context = context;
    }

    public async Task<Account?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Account?> GetByIdWithUserAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
    {
        _context.Accounts.Update(account);
        return Task.CompletedTask;
    }

    public async Task AddTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }
}
