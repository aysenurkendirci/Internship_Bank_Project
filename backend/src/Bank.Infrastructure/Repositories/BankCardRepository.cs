using Bank.Application.Abstractions.Repositories;
using Bank.Domain.Cards;
using Bank.Infrastructure.Persistence;
using Dapper;

namespace Bank.Infrastructure.Repositories;

public sealed class BankCardRepository : IBankCardRepository
{
    private readonly SqlConnectionFactory _connections;

    public BankCardRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<BankCard?> GetAsync(long userId, long cardId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT CardId, UserId, Status, Contactless, OnlineUse, DailyLimit, MonthlyLimit
            FROM dbo.Cards
            WHERE CardId = @CardId AND UserId = @UserId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var row = await connection.QuerySingleOrDefaultAsync<CardRow>(
            new CommandDefinition(sql, new { CardId = cardId, UserId = userId }, cancellationToken: ct));

        return row is null
            ? null
            : new BankCard(row.CardId, row.UserId, Enum.Parse<BankCardStatus>(row.Status),
                           row.Contactless, row.OnlineUse, row.DailyLimit, row.MonthlyLimit);
    }

    public async Task UpdateAsync(BankCard card, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE dbo.Cards
            SET Status = @Status, Contactless = @Contactless, OnlineUse = @OnlineUse,
                DailyLimit = @DailyLimit, MonthlyLimit = @MonthlyLimit
            WHERE CardId = @Id AND UserId = @UserId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        await connection.ExecuteAsync(new CommandDefinition(sql, new
        {
            card.Id,
            card.UserId,
            Status = card.Status.ToString(),
            card.Contactless,
            card.OnlineUse,
            card.DailyLimit,
            card.MonthlyLimit
        }, cancellationToken: ct));
    }

    private sealed class CardRow
    {
        public long CardId { get; init; }
        public long UserId { get; init; }
        public string Status { get; init; } = string.Empty;
        public bool Contactless { get; init; }
        public bool OnlineUse { get; init; }
        public decimal DailyLimit { get; init; }
        public decimal MonthlyLimit { get; init; }
    }
}
