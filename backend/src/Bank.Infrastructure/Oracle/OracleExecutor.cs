using Dapper;
using System.Data;

namespace Bank.Infrastructure.Oracle;

public sealed class OracleExecutor
{
    private readonly OracleConnectionFactory _factory;

    public OracleExecutor(OracleConnectionFactory factory)
    {
        _factory = factory;
    }

    // Bağlantı testi (istersen kalsın, şart değil)
    public async Task TestConnectionAsync()
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();
    }

    public async Task<T?> QuerySingleAsync<T>(string procName, object? param = null)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        return await conn.QuerySingleOrDefaultAsync<T>(
            procName,
            param,
            commandType: CommandType.StoredProcedure
        );
    }

    // ✅ Stored Procedure Execute
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
}
