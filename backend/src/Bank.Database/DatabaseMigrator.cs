using DbUp;
using DbUp.Engine;

namespace Bank.Database;

/// <summary>
/// Brings a database to the latest schema version. Used by the console entry point and by the
/// integration tests, so the tests always run against the exact scripts that ship.
/// </summary>
public static class DatabaseMigrator
{
    public static DatabaseUpgradeResult Migrate(string connectionString, bool logToConsole = true)
    {
        // Creating the database is a local-development convenience. In shared environments
        // the database is provisioned upfront and this login would not have CREATE DATABASE rights.
        EnsureDatabase.For.SqlDatabase(connectionString);

        var builder = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .WithTransactionPerScript();

        builder = logToConsole ? builder.LogToConsole() : builder.LogToNowhere();

        return builder.Build().PerformUpgrade();
    }
}
