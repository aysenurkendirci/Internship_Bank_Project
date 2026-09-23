using Bank.Domain.Entities;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Abstractions.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<User?> GetByTcNoAsync(TcNo tcNo, CancellationToken cancellationToken = default);
    Task<User?> GetByIdWithDetailsAsync(long id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
}
