using Dapper;
using System.Data;

namespace Bank.Infrastructure.Oracle;

public sealed class OracleExecutor
{
    private readonly OracleConnectionFactory _factory;

    public OracleExecutor(OracleConnectionFactory factory)
    {
        _factory = factory;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public async Task<T?> QuerySingleAsync<T>(string procName, SqlMapper.IDynamicParameters? param = null)
    {
        using var conn = _factory.CreateConnection();
        await conn.OpenAsync();

        return await conn.QueryFirstOrDefaultAsync<T>(
            procName,
            param,
            commandType: CommandType.StoredProcedure
        );
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(string procName, SqlMapper.IDynamicParameters? param = null)
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

    public async Task<int> ExecuteAsync(string procName, SqlMapper.IDynamicParameters? param = null)
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
