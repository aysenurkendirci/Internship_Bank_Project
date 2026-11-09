using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Auth;

/// <summary>
/// Exchanges a refresh token for a new access token and a new refresh token. The result is null when the
/// token cannot be used; the caller answers with the same 401 as a failed sign-in.
/// </summary>
/// <remarks>
/// A rejected token is a result rather than an exception on purpose: an exception would roll the transaction
/// back, and when a reused token revokes every session, that revocation must be kept.
/// </remarks>
public sealed record RefreshSessionCommand(string? RefreshToken) : IRequest<AuthResult?>, ITransactionalCommand;

public sealed class RefreshSessionCommandHandler : IRequestHandler<RefreshSessionCommand, AuthResult?>
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly IAuthRepository _users;
    private readonly ITokenGenerator _accessTokens;
    private readonly RefreshTokenIssuer _refreshTokens;
    private readonly TimeProvider _time;
    private readonly ILogger<RefreshSessionCommandHandler> _logger;

    public RefreshSessionCommandHandler(
        IRefreshTokenRepository tokens,
        IAuthRepository users,
        ITokenGenerator accessTokens,
        RefreshTokenIssuer refreshTokens,
        TimeProvider time,
        ILogger<RefreshSessionCommandHandler> logger)
    {
        _tokens = tokens;
        _users = users;
        _accessTokens = accessTokens;
        _refreshTokens = refreshTokens;
        _time = time;
        _logger = logger;
    }

    public async Task<AuthResult?> Handle(RefreshSessionCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
            return null;

        var hash = RefreshTokenIssuer.Hash(command.RefreshToken);
        var now = _time.GetUtcNow().UtcDateTime;

        var consumed = await _tokens.TryConsumeAsync(hash, now, ct);
        if (consumed is null)
        {
            await RevokeEverythingIfReusedAsync(hash, now, ct);
            return null;
        }

        var profile = await _users.GetProfileAsync(consumed.UserId, ct);
        var next = await _refreshTokens.IssueAsync(consumed.UserId, ct);
        await _tokens.SetReplacementAsync(consumed.TokenId, next.TokenId, ct);

        return new AuthResult(
            new(consumed.UserId, $"{profile.FirstName} {profile.LastName}", _accessTokens.Generate(consumed.UserId)),
            next);
    }

    // A token that was already used and is presented again means two parties hold a copy. Which of them is the
    // real user cannot be told, so every session of the user ends and they sign in again.
    private async Task RevokeEverythingIfReusedAsync(byte[] hash, DateTime now, CancellationToken ct)
    {
        if (await _tokens.FindOwnerOfReplacedAsync(hash, ct) is not { } userId)
            return;

        await _tokens.RevokeAllAsync(userId, now, ct);
        _logger.LogWarning("A used refresh token was presented again; all sessions of user {UserId} were revoked", userId);
    }
}
