using Bank.Infrastructure.Persistence;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bank.Api.Health;

/// <summary>
/// Readiness: the API is only useful if it can reach its database. The check opens a connection and runs
/// a trivial query; it never exposes connection details in its result.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly SqlConnectionFactory _connections;

    public DatabaseHealthCheck(SqlConnectionFactory connections) => _connections = connections;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var connection = await _connections.OpenAsync(ct);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: ct));
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            // The exception goes to the health check log; the HTTP response only says "Unhealthy".
            return HealthCheckResult.Unhealthy("Database is not reachable.", ex);
        }
    }
}
