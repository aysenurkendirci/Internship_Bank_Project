using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Cards;
using Bank.Domain.ValueObjects;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories.Finance;

/// <summary>Installment plans; schedules are calculated, so only the purchase itself is stored.</summary>
public sealed class InstallmentPlanRepository : IInstallmentPlanRepository
{
    private const string SelectColumns = """
        SELECT InstallmentPlanId, CreditCardId, Description, TotalAmount, CurrencyCode, InstallmentCount, PurchaseDate
        FROM finance.InstallmentPlans
        """;

    private readonly SqlConnectionFactory _connections;

    public InstallmentPlanRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<IReadOnlyList<InstallmentPlan>> ListAsync(long userId, CancellationToken ct = default)
    {
        const string sql = SelectColumns + """

            WHERE UserId = @UserId AND DeletedAt IS NULL
            ORDER BY PurchaseDate, InstallmentPlanId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<InstallmentPlanRow>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: ct));

        return rows.Select(ToDomain).ToList();
    }

    public async Task<InstallmentPlan?> GetAsync(long userId, long id, CancellationToken ct = default)
    {
        const string sql = SelectColumns + """

            WHERE InstallmentPlanId = @Id AND UserId = @UserId AND DeletedAt IS NULL
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<InstallmentPlanRow>(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));

        return row is null ? null : ToDomain(row);
    }

    public async Task<InstallmentPlan> AddAsync(long userId, InstallmentPlan plan, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO finance.InstallmentPlans
                (UserId, CreditCardId, Description, TotalAmount, CurrencyCode, InstallmentCount, PurchaseDate)
            OUTPUT INSERTED.InstallmentPlanId, INSERTED.CreditCardId, INSERTED.Description, INSERTED.TotalAmount,
                   INSERTED.CurrencyCode, INSERTED.InstallmentCount, INSERTED.PurchaseDate
            VALUES
                (@UserId, @CreditCardId, @Description, @TotalAmount, @CurrencyCode, @InstallmentCount, @PurchaseDate)
            """;

        var parameters = new
        {
            UserId = userId,
            plan.CreditCardId,
            plan.Description,
            TotalAmount = plan.TotalAmount.Amount,
            CurrencyCode = plan.TotalAmount.Currency.Code,
            plan.InstallmentCount,
            PurchaseDate = plan.PurchaseDate.ToDateTime(TimeOnly.MinValue)
        };

        await using var connection = await _connections.OpenAsync(ct);
        var row = await connection.QuerySingleAsync<InstallmentPlanRow>(
            new CommandDefinition(sql, parameters, cancellationToken: ct));

        return ToDomain(row);
    }

    public async Task<bool> SoftDeleteAsync(long userId, long id, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE finance.InstallmentPlans
            SET DeletedAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME()
            WHERE InstallmentPlanId = @Id AND UserId = @UserId AND DeletedAt IS NULL
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var affected = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id, UserId = userId }, cancellationToken: ct));

        return affected == 1;
    }

    private static InstallmentPlan ToDomain(InstallmentPlanRow row)
        => new(
            row.InstallmentPlanId,
            row.CreditCardId,
            row.Description,
            new Money(row.TotalAmount, Currency.FromCode(row.CurrencyCode)),
            row.InstallmentCount,
            DateOnly.FromDateTime(row.PurchaseDate));

    private sealed class InstallmentPlanRow
    {
        public long InstallmentPlanId { get; init; }
        public long CreditCardId { get; init; }
        public string Description { get; init; } = "";
        public decimal TotalAmount { get; init; }
        public string CurrencyCode { get; init; } = "";
        public byte InstallmentCount { get; init; }
        public DateTime PurchaseDate { get; init; }
    }
}
