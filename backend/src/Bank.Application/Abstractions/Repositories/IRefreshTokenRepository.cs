namespace Bank.Application.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task<long> AddAsync(long userId, byte[] tokenHash, DateTime createdAt, DateTime expiresAt, CancellationToken ct = default);

    /// <summary>
    /// Revokes the token if it is still valid, in a single statement so two requests presenting the same token
    /// cannot both succeed. Returns null when the token is unknown, expired or already revoked.
    /// </summary>
    Task<ConsumedToken?> TryConsumeAsync(byte[] tokenHash, DateTime now, CancellationToken ct = default);

    Task SetReplacementAsync(long tokenId, long replacedByTokenId, CancellationToken ct = default);

    /// <summary>
    /// The owner of a token that was already exchanged for a new one, or null otherwise. A token revoked by a
    /// logout or a password change has no replacement: seeing it again is just a stale device, not a stolen copy.
    /// </summary>
    Task<long?> FindOwnerOfReplacedAsync(byte[] tokenHash, CancellationToken ct = default);

    Task RevokeAllAsync(long userId, DateTime now, CancellationToken ct = default);

    /// <summary>Revokes one token if it is open; unknown or already revoked tokens are ignored.</summary>
    Task RevokeAsync(byte[] tokenHash, DateTime now, CancellationToken ct = default);
}

public sealed record ConsumedToken(long TokenId, long UserId);
