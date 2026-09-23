using Bank.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Bank.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BankDbContext _context;

    public UnitOfWork(BankDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
