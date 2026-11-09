using Bank.Application.Abstractions.Repositories;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories;

/// <remarks>
/// Uses the request's <see cref="DbSession"/>, so a refresh runs in the transaction the pipeline opened:
/// the old token is revoked and its replacement stored together, or neither is.
/// </remarks>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DbSession _session;

    public RefreshTokenRepository(DbSession session) => _session = session;

    public Task<long> AddAsync(long userId, byte[] tokenHash, DateTime createdAt, DateTime expiresAt, CancellationToken ct = default)
        => ScalarAsync<long>("""
            INSERT INTO dbo.RefreshTokens (UserId, TokenHash, CreatedAt, ExpiresAt)
            OUTPUT INSERTED.TokenId
            VALUES (@UserId, @TokenHash, @CreatedAt, @ExpiresAt)
            """,
            new { UserId = userId, TokenHash = tokenHash, CreatedAt = createdAt, ExpiresAt = expiresAt }, ct);

    public async Task<ConsumedToken?> TryConsumeAsync(byte[] tokenHash, DateTime now, CancellationToken ct = default)
    {
        // One statement decides the winner: of two requests with the same token, only one finds it still open.
        var connection = await _session.GetConnectionAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<ConsumedToken>(new CommandDefinition("""
            UPDATE dbo.RefreshTokens
            SET RevokedAt = @Now
            OUTPUT INSERTED.TokenId, INSERTED.UserId
            WHERE TokenHash = @TokenHash AND RevokedAt IS NULL AND ExpiresAt > @Now
            """,
            new { TokenHash = tokenHash, Now = now }, _session.Transaction, cancellationToken: ct));
    }

    public async Task SetReplacementAsync(long tokenId, long replacedByTokenId, CancellationToken ct = default)
        => await ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET ReplacedByTokenId = @ReplacedBy WHERE TokenId = @TokenId",
            new { TokenId = tokenId, ReplacedBy = replacedByTokenId }, ct);

    public async Task<long?> FindOwnerOfReplacedAsync(byte[] tokenHash, CancellationToken ct = default)
        => await ScalarAsync<long?>(
            "SELECT UserId FROM dbo.RefreshTokens WHERE TokenHash = @TokenHash AND ReplacedByTokenId IS NOT NULL",
            new { TokenHash = tokenHash }, ct);

    public async Task RevokeAllAsync(long userId, DateTime now, CancellationToken ct = default)
        => await ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET RevokedAt = @Now WHERE UserId = @UserId AND RevokedAt IS NULL",
            new { UserId = userId, Now = now }, ct);

    public async Task RevokeAsync(byte[] tokenHash, DateTime now, CancellationToken ct = default)
        => await ExecuteAsync(
            "UPDATE dbo.RefreshTokens SET RevokedAt = @Now WHERE TokenHash = @TokenHash AND RevokedAt IS NULL",
            new { TokenHash = tokenHash, Now = now }, ct);

    private async Task<T?> ScalarAsync<T>(string sql, object parameters, CancellationToken ct)
    {
        var connection = await _session.GetConnectionAsync(ct);
        return await connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, _session.Transaction, cancellationToken: ct));
    }

    private async Task ExecuteAsync(string sql, object parameters, CancellationToken ct)
    {
        var connection = await _session.GetConnectionAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, parameters, _session.Transaction, cancellationToken: ct));
    }
}
