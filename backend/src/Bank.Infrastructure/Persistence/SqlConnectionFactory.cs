using Microsoft.Data.SqlClient;

namespace Bank.Infrastructure.Persistence;

/// <summary>
/// Hands out open connections to the application database.
/// Connections are short-lived: open per operation, dispose immediately;
/// SqlClient's built-in pooling makes this cheap.
/// </summary>
public sealed class SqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }
}
