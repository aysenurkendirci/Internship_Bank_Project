using Bank.Domain.Accounts;
using Microsoft.Data.SqlClient;

namespace Bank.IntegrationTests.Infrastructure;

public static class TestData
{
    /// <summary>
    /// Accounts are opened empty through the API and there is no deposit endpoint, so funded accounts are seeded
    /// directly. The IBAN is valid, but uses a different bank code than the API so the two can never collide.
    /// </summary>
    public static async Task<(long AccountId, string Iban)> AddAccountAsync(long userId, decimal balance, string currencyCode = "TRY")
    {
        var iban = Iban.For("99998", Random.Shared.NextInt64(1, 9_999_999_999_999_999)).Value;

        await using var connection = new SqlConnection(ApiFactory.ConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO dbo.Accounts (UserId, AccountType, CurrencyCode, Iban, Balance)
            OUTPUT INSERTED.AccountId
            VALUES (@UserId, CASE WHEN @CurrencyCode = 'TRY' THEN 'Demand' ELSE 'ForeignCurrency' END, @CurrencyCode, @Iban, @Balance)
            """;
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@CurrencyCode", currencyCode);
        command.Parameters.AddWithValue("@Iban", iban);
        command.Parameters.AddWithValue("@Balance", balance);

        return ((long)(await command.ExecuteScalarAsync())!, iban);
    }

    public static Task<(long AccountId, string Iban)> AddTryAccountAsync(long userId, decimal balance)
        => AddAccountAsync(userId, balance);
}
