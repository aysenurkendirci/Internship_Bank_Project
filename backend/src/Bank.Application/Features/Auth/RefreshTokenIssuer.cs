using System.Security.Cryptography;
using System.Text;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;

namespace Bank.Application.Features.Auth;

/// <summary>The token to hand to the client, shown only once; the database keeps just its hash.</summary>
public sealed record IssuedRefreshToken(string Value, DateTime ExpiresAt, long TokenId);

public sealed class RefreshTokenIssuer
{
    private const int TokenBytes = 32;

    private readonly IRefreshTokenRepository _tokens;
    private readonly ISessionPolicy _policy;
    private readonly TimeProvider _time;

    public RefreshTokenIssuer(IRefreshTokenRepository tokens, ISessionPolicy policy, TimeProvider time)
    {
        _tokens = tokens;
        _policy = policy;
        _time = time;
    }

    public async Task<IssuedRefreshToken> IssueAsync(long userId, CancellationToken ct)
    {
        // 256 random bits: guessing one is not feasible, so a fast hash is enough (unlike passwords).
        var value = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var now = _time.GetUtcNow().UtcDateTime;
        var expiresAt = now + _policy.RefreshTokenLifetime;
        var id = await _tokens.AddAsync(userId, Hash(value), now, expiresAt, ct);

        return new IssuedRefreshToken(value, expiresAt, id);
    }

    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
