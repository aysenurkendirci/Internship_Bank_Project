using Bank.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

namespace Bank.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in memory against a dedicated SQL Server database that is dropped and
/// rebuilt from the shipped migrations once per test run. Nothing is mocked: routing, auth,
/// validation, SQL and constraints all run as they do in production.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string DatabaseName = "BankDb_IntegrationTests";

    /// <summary>
    /// The API's clock is pinned so date-dependent results (projections) are reproducible:
    /// 09:00 UTC on 31 March 2026 is 12:00 in Istanbul, so "today" is 31 March.
    /// </summary>
    public static readonly DateTimeOffset Now = new(2026, 3, 31, 9, 0, 0, TimeSpan.Zero);

    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("INTEGRATION_TESTS_DB")
        ?? $"Server=localhost;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True";

    public Task InitializeAsync()
    {
        DropDatabase();

        var result = DatabaseMigrator.Migrate(ConnectionString, logToConsole: false);
        if (!result.Successful)
            throw new InvalidOperationException("Test database migration failed.", result.Error);

        return Task.CompletedTask;
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" keeps user-secrets and developer-only middleware out of the picture.
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:BankDb", ConnectionString);
        builder.UseSetting("Jwt:Key", "integration-tests-signing-key-0123456789abcdef");
        // The suite registers and logs in many users from one address; the limit itself is tested in SecurityTests.
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");
        builder.ConfigureTestServices(services => services.AddSingleton<TimeProvider>(new FakeTimeProvider(Now)));
    }

    private static void DropDatabase()
    {
        var master = new SqlConnectionStringBuilder(ConnectionString) { InitialCatalog = "master" };

        using var connection = new SqlConnection(master.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID('{DatabaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DatabaseName}];
            END
            """;
        command.ExecuteNonQuery();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
