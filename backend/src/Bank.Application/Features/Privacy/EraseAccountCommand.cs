using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Privacy;

/// <summary>Erases the signed-in customer (KVKK right to erasure). Final: there is no way back.</summary>
public sealed record EraseAccountCommand(string Password) : IRequest;

public sealed class EraseAccountCommandHandler : IRequestHandler<EraseAccountCommand>
{
    private readonly IAuthRepository _users;
    private readonly IAccountRepository _accounts;
    private readonly IErasureRepository _erasure;
    private readonly IPasswordHasher _hasher;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;
    private readonly ILogger<EraseAccountCommandHandler> _logger;

    public EraseAccountCommandHandler(
        IAuthRepository users,
        IAccountRepository accounts,
        IErasureRepository erasure,
        IPasswordHasher hasher,
        ICurrentUser currentUser,
        TimeProvider time,
        ILogger<EraseAccountCommandHandler> logger)
    {
        _users = users;
        _accounts = accounts;
        _erasure = erasure;
        _hasher = hasher;
        _currentUser = currentUser;
        _time = time;
        _logger = logger;
    }

    public async Task Handle(EraseAccountCommand command, CancellationToken ct)
    {
        var userId = _currentUser.UserId;

        var credentials = await _users.GetCredentialsAsync(userId, ct);
        if (!_hasher.Verify(command.Password, credentials.PasswordHash))
        {
            _logger.LogWarning("Erasure refused for user {UserId}: wrong password", userId);
            throw new BadRequestException("Şifre doğrulanamadı.");
        }

        // The procedure checks this again inside its transaction; asking here gives the customer a clear message.
        var accounts = await _accounts.ListAsync(userId, ct);
        if (accounts.Any(a => a.Balance != 0))
            throw new ConflictException("Hesaplarınızda bakiye var. Hesabınızı silmeden önce paranızı başka bir hesaba aktarın.");

        await _erasure.EraseAsync(userId, _time.GetUtcNow().UtcDateTime, ct);

        // No name or identity number: only that an erasure happened.
        _logger.LogInformation("User {UserId} was erased on request", userId);
    }
}
