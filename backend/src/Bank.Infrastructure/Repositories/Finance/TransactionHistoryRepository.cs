using System.Data;
using Bank.Application.Abstractions.Repositories;
using Bank.Infrastructure.Persistence;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Bank.Infrastructure.Repositories.Finance;

public sealed class TransactionHistoryRepository : ITransactionHistoryRepository
{
    private readonly SqlConnectionFactory _connections;

    public TransactionHistoryRepository(SqlConnectionFactory connections) => _connections = connections;

    public async Task<ImportOutcome> ImportAsync(
        long userId, string source, string? fileName, IReadOnlyList<HistoryTransaction> transactions,
        CancellationToken ct = default)
    {
        var batchId = Guid.NewGuid();

        await using var connection = await _connections.OpenAsync(ct);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct);

        // Rows are bulk-copied into a session temp table and merged with one statement instead of
        // thousands of single INSERT round trips.
        await connection.ExecuteAsync(new CommandDefinition("""
            CREATE TABLE #Staging
            (
                BookingDate  DATE           NOT NULL,
                Description  NVARCHAR(200)  NOT NULL,
                Amount       DECIMAL(19,4)  NOT NULL,
                Direction    VARCHAR(10)    NOT NULL,
                CurrencyCode CHAR(3)        NOT NULL,
                Category     NVARCHAR(50)   NULL,
                Fingerprint  BINARY(32)     NOT NULL
            );
            """, transaction: transaction, cancellationToken: ct));

        using (var bulk = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction) { DestinationTableName = "#Staging" })
            await bulk.WriteToServerAsync(ToStagingTable(transactions), ct);

        // UPDLOCK + HOLDLOCK keep a concurrent upload of the same file from slipping in between the
        // duplicate check and the insert; the unique index would reject it, but with an error.
        var duplicates = await connection.ExecuteScalarAsync<int>(new CommandDefinition("""
            SELECT COUNT(*)
            FROM #Staging s
            WHERE EXISTS (SELECT 1 FROM finance.Transactions t WITH (UPDLOCK, HOLDLOCK)
                          WHERE t.UserId = @UserId AND t.Fingerprint = s.Fingerprint AND t.DeletedAt IS NULL)
            """, new { UserId = userId }, transaction, cancellationToken: ct));

        var imported = transactions.Count - duplicates;

        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO finance.ImportBatches (ImportBatchId, UserId, Source, FileName, TotalRows, ImportedRows, DuplicateRows)
            VALUES (@BatchId, @UserId, @Source, @FileName, @Total, @Imported, @Duplicates);

            INSERT INTO finance.Transactions
                (UserId, ImportBatchId, BookingDate, Description, Amount, Direction, CurrencyCode, Category, Fingerprint)
            SELECT @UserId, @BatchId, s.BookingDate, s.Description, s.Amount, s.Direction, s.CurrencyCode, s.Category, s.Fingerprint
            FROM #Staging s
            WHERE NOT EXISTS (SELECT 1 FROM finance.Transactions t
                              WHERE t.UserId = @UserId AND t.Fingerprint = s.Fingerprint AND t.DeletedAt IS NULL);
            """,
            new { BatchId = batchId, UserId = userId, Source = source, FileName = fileName,
                  Total = transactions.Count, Imported = imported, Duplicates = duplicates },
            transaction, cancellationToken: ct));

        await transaction.CommitAsync(ct);

        return new ImportOutcome(batchId, transactions.Count, imported, duplicates);
    }

    public async Task<IReadOnlyList<StoredHistoryTransaction>> ListRangeAsync(
        long userId, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TransactionId, BookingDate, Description, Amount, Direction, CurrencyCode, Category
            FROM finance.Transactions
            WHERE UserId = @UserId AND DeletedAt IS NULL AND BookingDate BETWEEN @From AND @To
            ORDER BY BookingDate, TransactionId
            """;

        await using var connection = await _connections.OpenAsync(ct);
        var rows = await connection.QueryAsync<HistoryRow>(new CommandDefinition(sql,
            new { UserId = userId, From = from.ToDateTime(TimeOnly.MinValue), To = to.ToDateTime(TimeOnly.MinValue) },
            cancellationToken: ct));

        return rows.Select(ToStored).ToList();
    }

    public async Task<(IReadOnlyList<StoredHistoryTransaction> Items, int TotalCount)> ListAsync(
        long userId, DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM finance.Transactions
            WHERE UserId = @UserId AND DeletedAt IS NULL
              AND (@From IS NULL OR BookingDate >= @From)
              AND (@To IS NULL OR BookingDate <= @To);

            SELECT TransactionId, BookingDate, Description, Amount, Direction, CurrencyCode, Category
            FROM finance.Transactions
            WHERE UserId = @UserId AND DeletedAt IS NULL
              AND (@From IS NULL OR BookingDate >= @From)
              AND (@To IS NULL OR BookingDate <= @To)
            ORDER BY BookingDate DESC, TransactionId DESC
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
            """;

        var parameters = new
        {
            UserId = userId,
            From = from?.ToDateTime(TimeOnly.MinValue),
            To = to?.ToDateTime(TimeOnly.MinValue),
            Skip = (page - 1) * pageSize,
            Take = pageSize
        };

        await using var connection = await _connections.OpenAsync(ct);
        await using var results = await connection.QueryMultipleAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));

        var total = await results.ReadSingleAsync<int>();
        var rows = await results.ReadAsync<HistoryRow>();

        return (rows.Select(ToStored).ToList(), total);
    }

    private static StoredHistoryTransaction ToStored(HistoryRow r)
        => new(
            r.TransactionId,
            DateOnly.FromDateTime(r.BookingDate),
            r.Description,
            r.Direction == "Inbound" ? r.Amount : -r.Amount,
            r.CurrencyCode,
            r.Category);

    private static DataTable ToStagingTable(IEnumerable<HistoryTransaction> transactions)
    {
        var table = new DataTable();
        table.Columns.Add("BookingDate", typeof(DateTime));
        table.Columns.Add("Description", typeof(string));
        table.Columns.Add("Amount", typeof(decimal));
        table.Columns.Add("Direction", typeof(string));
        table.Columns.Add("CurrencyCode", typeof(string));
        table.Columns.Add("Category", typeof(string));
        table.Columns.Add("Fingerprint", typeof(byte[]));

        // Stored like dbo.Transactions: positive amount, sign carried by Direction.
        foreach (var t in transactions)
            table.Rows.Add(
                t.Date.ToDateTime(TimeOnly.MinValue),
                t.Description,
                Math.Abs(t.Amount),
                t.Amount > 0 ? "Inbound" : "Outbound",
                t.CurrencyCode,
                (object?)t.Category ?? DBNull.Value,
                t.Fingerprint);

        return table;
    }

    private sealed class HistoryRow
    {
        public long TransactionId { get; init; }
        public DateTime BookingDate { get; init; }
        public string Description { get; init; } = "";
        public decimal Amount { get; init; }
        public string Direction { get; init; } = "";
        public string CurrencyCode { get; init; } = "";
        public string? Category { get; init; }
    }
}
