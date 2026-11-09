using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Contracts.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Auth;

public sealed record LoginCommand(LoginRequest Credentials) : IRequest<AuthResult>;

/// <remarks>
/// Every sign-in attempt is logged as a security event so brute force and credential stuffing can be
/// spotted. The client always gets the same "wrong identity number or password" answer, but the log
/// keeps the real reason. Identity numbers are masked before they reach the log.
/// </remarks>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _tokens;
    private readonly RefreshTokenIssuer _refreshTokens;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IAuthRepository repo, IPasswordHasher hasher, ITokenGenerator tokens, RefreshTokenIssuer refreshTokens,
        ILogger<LoginCommandHandler> logger)
    {
        _repo = repo;
        _hasher = hasher;
        _tokens = tokens;
        _refreshTokens = refreshTokens;
        _logger = logger;
    }

    public async Task<AuthResult> Handle(LoginCommand command, CancellationToken ct)
    {
        var credentials = command.Credentials;
        var maskedTc = PersonalData.MaskTcKimlikNo(credentials.TcNo);

        var user = await _repo.GetUserByTcOrDefaultAsync(credentials.TcNo, ct);
        if (user is null)
        {
            _logger.LogWarning("Login failed for {TcNo}: unknown identity number", maskedTc);
            throw new AuthenticationFailedException();
        }

        var stored = await _repo.GetCredentialsAsync(user.UserId, ct);
        if (!_hasher.Verify(credentials.Password, stored.PasswordHash))
        {
            _logger.LogWarning("Login failed for user {UserId} ({TcNo}): wrong password", user.UserId, maskedTc);
            throw new AuthenticationFailedException();
        }

        await _repo.UpdateLastLoginAsync(user.UserId, ct);

        _logger.LogInformation("User {UserId} logged in", user.UserId);
        return await AuthResponses.CreateAsync(user.UserId, $"{user.FirstName} {user.LastName}", _tokens, _refreshTokens, ct);
    }
}
