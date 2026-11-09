using Bank.Application.Abstractions.Repositories;
using Bank.Application.Common;
using Bank.Domain.CashFlows;
using Bank.Domain.Scheduling;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories.Finance;

/// <summary>Recurring incomes, expenses and subscriptions.</summary>
public sealed class RecurringCashFlowRepository : IRecurringCashFlowRepository
{
    private const string SelectColumns = """
        SELECT RecurringCashFlowId, Name, Kind, Category, Amount, CurrencyCode,
               Frequency, RecurrenceInterval, StartDate, EndDate, CreditCardId, RowVersion
        FROM finance.RecurringCashFlows
        """;

    private readonly SqlConnectionFactory _connections;

    public RecurringCashFlowRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<IReadOnlyList<Versioned<RecurringCashFlow>>> ListAsync(
        long userId, CashFlowKind? kind, CancellationToken ct = default)
    {
        const string sql = SelectColumns + """

            WHERE UserId = @UserId AND DeletedAt IS NULL
              AND (@Kind IS NULL OR Kind = @Kind)
            ORDER BY StartDate, RecurringCashFlowId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<RecurringCashFlowRow>(
            new CommandDefinition(sql, new { UserId = userId, Kind = kind?.ToString() }, cancellationToken: ct));

        return rows.Select(ToVersioned).ToList();
    }

    public async Task<Versioned<RecurringCashFlow>?> GetAsync(long userId, long id, CancellationToken ct = default)
    {
        const string sql = SelectColumns + """

            WHERE RecurringCashFlowId = @Id AND UserId = @UserId AND DeletedAt IS NULL
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<RecurringCashFlowRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));

        return row is null ? null : ToVersioned(row);
    }

    public async Task<Versioned<RecurringCashFlow>> AddAsync(
        long userId, RecurringCashFlow item, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO finance.RecurringCashFlows
                (UserId, CreditCardId, Name, Kind, Category, Amount, CurrencyCode,
                 Frequency, RecurrenceInterval, StartDate, EndDate)
            OUTPUT INSERTED.RecurringCashFlowId, INSERTED.Name, INSERTED.Kind, INSERTED.Category,
                   INSERTED.Amount, INSERTED.CurrencyCode, INSERTED.Frequency, INSERTED.RecurrenceInterval,
                   INSERTED.StartDate, INSERTED.EndDate, INSERTED.CreditCardId, INSERTED.RowVersion
            VALUES
                (@UserId, @CreditCardId, @Name, @Kind, @Category, @Amount, @CurrencyCode,
                 @Frequency, @RecurrenceInterval, @StartDate, @EndDate)
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var row = await connection.QuerySingleAsync<RecurringCashFlowRow>(
            new CommandDefinition(sql, ToParameters(userId, item), cancellationToken: ct));

        return ToVersioned(row);
    }

    public async Task<byte[]?> UpdateAsync(
        long userId, RecurringCashFlow item, byte[] expectedVersion, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE finance.RecurringCashFlows
            SET CreditCardId = @CreditCardId,
                Name = @Name,
                Kind = @Kind,
                Category = @Category,
                Amount = @Amount,
                CurrencyCode = @CurrencyCode,
                Frequency = @Frequency,
                RecurrenceInterval = @RecurrenceInterval,
                StartDate = @StartDate,
                EndDate = @EndDate,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT INSERTED.RowVersion
            WHERE RecurringCashFlowId = @Id AND UserId = @UserId AND DeletedAt IS NULL
              AND RowVersion = @ExpectedVersion
            """;

        var parameters = new DynamicParameters(ToParameters(userId, item));
        parameters.Add("Id", item.Id);
        parameters.Add("ExpectedVersion", expectedVersion);

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<byte[]>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<bool> SoftDeleteAsync(long userId, long id, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE finance.RecurringCashFlows
            SET DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
            WHERE RecurringCashFlowId = @Id AND UserId = @UserId AND DeletedAt IS NULL
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
                SELECT 1 FROM finance.RecurringCashFlows
                WHERE RecurringCashFlowId = @Id AND UserId = @UserId AND DeletedAt IS NULL)
            THEN 1 ELSE 0 END
            """;

        await using var connection = await _connections.OpenAsync(ct);
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));
    }

    private static object ToParameters(long userId, RecurringCashFlow item) => new
    {
        UserId = userId,
        item.CreditCardId,
        item.Name,
        Kind = item.Kind.ToString(),
        item.Category,
        Amount = item.Amount.Amount,
        CurrencyCode = item.Amount.Currency.Code,
        Frequency = item.Recurrence.Frequency.ToString(),
        RecurrenceInterval = item.Recurrence.Interval,
        StartDate = item.Recurrence.StartDate.ToDateTime(TimeOnly.MinValue),
        EndDate = item.Recurrence.EndDate?.ToDateTime(TimeOnly.MinValue)
    };

    private static Versioned<RecurringCashFlow> ToVersioned(RecurringCashFlowRow row)
        => new(
            new RecurringCashFlow(
                row.RecurringCashFlowId,
                row.Name,
                Enum.Parse<CashFlowKind>(row.Kind),
                row.Category,
                new Money(row.Amount, Currency.FromCode(row.CurrencyCode)),
                new RecurrenceRule(
                    Enum.Parse<Frequency>(row.Frequency),
                    DateOnly.FromDateTime(row.StartDate),
                    row.RecurrenceInterval,
                    row.EndDate is { } end ? DateOnly.FromDateTime(end) : null),
                row.CreditCardId),
            row.RowVersion);

    // DATE columns are read as DateTime and converted, which works across Dapper versions.
    private sealed class RecurringCashFlowRow
    {
        public long RecurringCashFlowId { get; init; }
        public string Name { get; init; } = "";
        public string Kind { get; init; } = "";
        public string Category { get; init; } = "";
        public decimal Amount { get; init; }
        public string CurrencyCode { get; init; } = "";
        public string Frequency { get; init; } = "";
        public short RecurrenceInterval { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime? EndDate { get; init; }
        public long? CreditCardId { get; init; }
        public byte[] RowVersion { get; init; } = [];
    }
}
