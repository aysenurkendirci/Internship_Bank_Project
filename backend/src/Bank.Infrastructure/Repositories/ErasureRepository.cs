using System.Data;
using Bank.Application.Abstractions.Repositories;
using Bank.Infrastructure.Persistence;
using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace Bank.Infrastructure.Repositories;

public sealed class ErasureRepository : IErasureRepository
{
    // An access token stays valid until it expires, so every request would otherwise have to ask the database
    // whether its user was erased. The answer is cached briefly instead: an erased user can use an old token
    // for at most this long. (Erased users are cached much longer: that answer never changes back.)
    private static readonly TimeSpan NotErasedCacheTime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ErasedCacheTime = TimeSpan.FromHours(1);

    private readonly SqlConnectionFactory _connections;
    private readonly IMemoryCache _cache;

    public ErasureRepository(SqlConnectionFactory connections, IMemoryCache cache)
    {
        _connections = connections;
        _cache = cache;
    }

    public async Task EraseAsync(long userId, DateTime now, CancellationToken ct = default)
    {
        await using var connection = await _connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(
            "compliance.EraseUser",
            new { UserId = userId, Now = now },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));

        _cache.Set(CacheKey(userId), true, ErasedCacheTime);
    }

    public async Task<bool> IsErasedAsync(long userId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey(userId), out bool known))
            return known;

        await using var connection = await _connections.OpenAsync(ct);
        var erased = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM compliance.ErasedIdentities WHERE UserId = @UserId",
            new { UserId = userId },
            cancellationToken: ct)) > 0;

        _cache.Set(CacheKey(userId), erased, erased ? ErasedCacheTime : NotErasedCacheTime);
        return erased;
    }

    private static string CacheKey(long userId) => $"erased-user:{userId}";
}
