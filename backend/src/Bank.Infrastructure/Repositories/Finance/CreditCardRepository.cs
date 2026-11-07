using Bank.Application.Abstractions.Repositories;
using Bank.Application.Common;
using Bank.Domain.Cards;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories.Finance;

public sealed class CreditCardRepository : ICreditCardRepository
{
    private const string SelectColumns = """
        SELECT CreditCardId, Name, LimitAmount, CurrencyCode, StatementDay, PaymentDueDays, RowVersion
        FROM finance.CreditCards
        """;

    private readonly SqlConnectionFactory _connections;

    public CreditCardRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<IReadOnlyList<Versioned<CreditCard>>> ListAsync(long userId, CancellationToken ct = default)
    {
        const string sql = SelectColumns + """

            WHERE UserId = @UserId AND DeletedAt IS NULL
            ORDER BY CreditCardId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<CreditCardRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        return rows.Select(ToVersioned).ToList();
    }

    public async Task<Versioned<CreditCard>?> GetAsync(long userId, long id, CancellationToken ct = default)
    {
        const string sql = SelectColumns + """

            WHERE CreditCardId = @Id AND UserId = @UserId AND DeletedAt IS NULL
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<CreditCardRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));

        return row is null ? null : ToVersioned(row);
    }

    public async Task<Versioned<CreditCard>> AddAsync(long userId, CreditCard card, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO finance.CreditCards (UserId, Name, LimitAmount, CurrencyCode, StatementDay, PaymentDueDays)
            OUTPUT INSERTED.CreditCardId, INSERTED.Name, INSERTED.LimitAmount, INSERTED.CurrencyCode,
                   INSERTED.StatementDay, INSERTED.PaymentDueDays, INSERTED.RowVersion
            VALUES (@UserId, @Name, @LimitAmount, @CurrencyCode, @StatementDay, @PaymentDueDays)
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var row = await connection.QuerySingleAsync<CreditCardRow>(
            new CommandDefinition(sql, ToParameters(userId, card), cancellationToken: ct));

        return ToVersioned(row);
    }

    public async Task<byte[]?> UpdateAsync(
        long userId, CreditCard card, byte[] expectedVersion, CancellationToken ct = default)
    {
        // Matching on RowVersion makes the update conditional: if anyone changed the row since the
        // client read it, nothing is updated and the caller reports a conflict.
        const string sql = """
            UPDATE finance.CreditCards
            SET Name = @Name,
                LimitAmount = @LimitAmount,
                CurrencyCode = @CurrencyCode,
                StatementDay = @StatementDay,
                PaymentDueDays = @PaymentDueDays,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.RowVersion
            WHERE CreditCardId = @Id AND UserId = @UserId AND DeletedAt IS NULL
              AND RowVersion = @ExpectedVersion
            """;

        var parameters = new DynamicParameters(ToParameters(userId, card));
        parameters.Add("Id", card.Id);
        parameters.Add("ExpectedVersion", expectedVersion);

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<byte[]>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<bool> SoftDeleteIfUnusedAsync(long userId, long id, CancellationToken ct = default)
    {
        // The usage check lives in the same statement as the delete, so an installment added
        // between a separate check and the delete cannot end up pointing at a deleted card.
        const string sql = """
            UPDATE c
            SET DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
            FROM finance.CreditCards c
            WHERE c.CreditCardId = @Id AND c.UserId = @UserId AND c.DeletedAt IS NULL
              AND NOT EXISTS (SELECT 1 FROM finance.InstallmentPlans p
                              WHERE p.CreditCardId = c.CreditCardId AND p.DeletedAt IS NULL)
              AND NOT EXISTS (SELECT 1 FROM finance.RecurringCashFlows f
                              WHERE f.CreditCardId = c.CreditCardId AND f.DeletedAt IS NULL)
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));

        return affected == 1;
    }

    public async Task<bool> ExistsAsync(long userId, long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM finance.CreditCards
                WHERE CreditCardId = @Id AND UserId = @UserId AND DeletedAt IS NULL)
            THEN 1 ELSE 0 END
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));
    }

    public async Task<bool> IsInUseAsync(long userId, long id, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CASE WHEN
                EXISTS (SELECT 1 FROM finance.InstallmentPlans
                        WHERE CreditCardId = @Id AND UserId = @UserId AND DeletedAt IS NULL)
             OR EXISTS (SELECT 1 FROM finance.RecurringCashFlows
                        WHERE CreditCardId = @Id AND UserId = @UserId AND DeletedAt IS NULL)
            THEN 1 ELSE 0 END
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));
    }

    private static object ToParameters(long userId, CreditCard card) => new
    {
        UserId = userId,
        card.Name,
        LimitAmount = card.Limit.Amount,
        CurrencyCode = card.Limit.Currency.Code,
        card.StatementDay,
        card.PaymentDueDays
    };

    private static Versioned<CreditCard> ToVersioned(CreditCardRow row)
        => new(
            new CreditCard(
                row.CreditCardId,
                row.Name,
                new Money(row.LimitAmount, Currency.FromCode(row.CurrencyCode)),
                row.StatementDay,
                row.PaymentDueDays),
            row.RowVersion);

    private sealed class CreditCardRow
    {
        public long CreditCardId { get; init; }
        public string Name { get; init; } = "";
        public decimal LimitAmount { get; init; }
        public string CurrencyCode { get; init; } = "";
        public byte StatementDay { get; init; }
        public byte PaymentDueDays { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }
}
