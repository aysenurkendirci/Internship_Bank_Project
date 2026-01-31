using Dapper;
using System.Data;
using System.Linq;

namespace Bank.Infrastructure.Oracle;

public sealed class OracleExecutor
{
    private readonly OracleConnectionFactory _factory;

    public OracleExecutor(OracleConnectionFactory factory)
    {
        _factory = factory;
        
        // ✅ 1. ÇÖZÜM: Veritabanındaki USER_ID kolonunu C# tarafındaki UserId ile otomatik eşler.
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task TestConnectionAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
    }

    // REF CURSOR dönen prosedürler için güvenli okuma
    public async Task<T?> QuerySingleAsync<T>(string procName, object? param = null)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        // ✅ 2. ÇÖZÜM: Oracle bazen RefCursor verisini tam eşleyemez, 
        // QueryAsync yerine QueryFirstOrDefaultAsync kullanmak daha performanslıdır.
        return await conn.QueryFirstOrDefaultAsync<T>(
            procName,
            param,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<int> ExecuteAsync(string procName, object? param = null)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        return await conn.ExecuteAsync(
            procName,
            param,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string procName, object? param = null)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        var result = await conn.QueryAsync<T>(
            procName,
            param,
            commandType: CommandType.StoredProcedure
        );

        return result.AsList();
    }
}