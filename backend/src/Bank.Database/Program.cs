using Bank.Database;
using Microsoft.Extensions.Configuration;

// Applies pending migration scripts to the target database and exits.
// Scripts are embedded resources under Migrations/ and run in file-name order;
// DbUp records each applied script in dbo.SchemaVersions so it never runs twice.
//
// Usage: dotnet run --project src/Bank.Database
//        dotnet run --project src/Bank.Database -- --ConnectionStrings:BankDb="<connection string>"

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

var connectionString = configuration.GetConnectionString("BankDb")
    ?? throw new InvalidOperationException("Connection string 'BankDb' is missing.");

var result = DatabaseMigrator.Migrate(connectionString);

if (!result.Successful)
{
    Console.Error.WriteLine(result.Error);
    return 1;
}

Console.WriteLine("Database is up to date.");
return 0;
