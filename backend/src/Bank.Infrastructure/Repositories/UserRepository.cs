using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Entities;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bank.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly BankDbContext _context;

    public UserRepository(BankDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        // ValueObject'in içindeki asıl string değere (.Value) göre arama yapıyoruz.
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<User?> GetByTcNoAsync(TcNo tcNo, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.TcNo.Value == tcNo.Value, cancellationToken);
    }
}
