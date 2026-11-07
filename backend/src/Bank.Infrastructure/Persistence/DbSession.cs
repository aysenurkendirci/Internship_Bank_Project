using System.Data;
using Microsoft.Data.SqlClient;

namespace Bank.Infrastructure.Persistence;

/// <summary>
/// One connection per request scope, plus the transaction of the current unit of work if one is open.
/// Repositories that must take part in a unit of work read <see cref="Transaction"/> from here; the rest keep
/// opening short-lived connections through <see cref="SqlConnectionFactory"/>.
/// </summary>
public sealed class DbSession : IAsyncDisposable
{
    private readonly SqlConnectionFactory _connections;
    private SqlConnection? _connection;

    public DbSession(SqlConnectionFactory connections) => _connections = connections;

    public SqlTransaction? Transaction { get; private set; }

    public async Task<SqlConnection> GetConnectionAsync(CancellationToken ct)
        => _connection ??= await _connections.OpenAsync(ct);

    internal async Task<SqlTransaction> BeginTransactionAsync(CancellationToken ct)
    {
        if (Transaction is not null)
            throw new InvalidOperationException("A unit of work is already open in this scope; nesting is not supported.");

        var connection = await GetConnectionAsync(ct);
        Transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        return Transaction;
    }

    internal void EndTransaction() => Transaction = null;

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.DisposeAsync();
    }
}
