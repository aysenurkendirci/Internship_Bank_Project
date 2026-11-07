using Bank.Application.Abstractions.Repositories;
using MediatR;

namespace Bank.Application.Features.Auth;

/// <summary>Ends a session by revoking its refresh token on the server, so a stolen copy stops working too.</summary>
public sealed record LogoutCommand(string? RefreshToken) : IRequest;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly TimeProvider _time;

    public LogoutCommandHandler(IRefreshTokenRepository tokens, TimeProvider time)
    {
        _tokens = tokens;
        _time = time;
    }

    public async Task Handle(LogoutCommand command, CancellationToken ct)
    {
        // Signing out with no token, or with one that is already gone, is not an error: the outcome is the same.
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
            await _tokens.RevokeAsync(RefreshTokenIssuer.Hash(command.RefreshToken), _time.GetUtcNow().UtcDateTime, ct);
    }
}
