using Bank.Application.Abstractions.Repositories;
using Bank.Application.Imports;
using Bank.Contracts.Finance;

namespace Bank.Application.Features.TransactionHistory;

/// <summary>Stores statement lines with their fingerprints; shared by the CSV import and the demo generator.</summary>
internal static class HistoryImport
{
    // Statements carry no currency column; files are treated as TRY statements.
    private const string StatementCurrency = "TRY";

    public static async Task<ImportResultResponse> StoreAsync(
        ITransactionHistoryRepository history,
        long userId,
        string source,
        string? fileName,
        IReadOnlyList<StatementLine> lines,
        CancellationToken ct)
    {
        var fingerprints = TransactionFingerprint.For(lines);
        var transactions = lines
            .Select((line, i) => new HistoryTransaction(
                line.Date, line.Description, line.Amount, StatementCurrency, line.Category, fingerprints[i]))
            .ToList();

        var outcome = await history.ImportAsync(userId, source, fileName, transactions, ct);

        return new ImportResultResponse(outcome.BatchId, outcome.TotalRows, outcome.ImportedRows, outcome.DuplicateRows);
    }
}
