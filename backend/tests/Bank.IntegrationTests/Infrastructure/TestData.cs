using Microsoft.Data.SqlClient;

namespace Bank.IntegrationTests.Infrastructure;

public static class TestData
{
    /// <summary>There is no account API yet, so balances are seeded directly.</summary>
    public static async Task AddTryAccountAsync(long userId, decimal balance)
    {
        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Accounts (UserId, AccountType, CurrencyCode, Iban, Balance)
            VALUES (@UserId, 'Demand', 'TRY', LEFT(CONCAT('TR', REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '')), 26), @Balance)
            """;
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@Balance", balance);
        await command.ExecuteNonQueryAsync();
    }
}
