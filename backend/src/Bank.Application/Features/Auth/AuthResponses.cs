using Bank.Application.Abstractions.Security;
using Bank.Contracts.Auth;

namespace Bank.Application.Features.Auth;

/// <summary>What a successful sign-in hands back: the response body, plus the refresh token for the cookie.</summary>
public sealed record AuthResult(AuthResponse Response, IssuedRefreshToken RefreshToken);

internal static class AuthResponses
{
    public static async Task<AuthResult> CreateAsync(
        long userId, string fullName, ITokenGenerator tokens, RefreshTokenIssuer refreshTokens, CancellationToken ct)
        => new(new AuthResponse(userId, fullName, tokens.Generate(userId)), await refreshTokens.IssueAsync(userId, ct));
}
