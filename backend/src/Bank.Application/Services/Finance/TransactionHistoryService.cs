using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common;
using Bank.Application.Common.Exceptions;
using Bank.Application.Imports;
using Bank.Application.Synthetic;
using Bank.Contracts.Finance;

namespace Bank.Application.Services.Finance;

public sealed class TransactionHistoryService : ITransactionHistoryService
{
    // Statements carry no currency column; files are treated as TRY statements.
    private const string StatementCurrency = "TRY";
    private const int MaxReportedErrors = 50;

    private readonly ITransactionHistoryRepository _history;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public TransactionHistoryService(ITransactionHistoryRepository history, ICurrentUser currentUser, TimeProvider time)
    {
        _history = history;
        _currentUser = currentUser;
        _time = time;
    }

    /// <remarks>
    /// All or nothing: a file with any invalid line is rejected as a whole. A partial import would
    /// leave the user guessing which lines made it in, and fixing the file and re-uploading would
    /// then depend on duplicate detection to clean up after it.
    /// </remarks>
    public async Task<ImportResultResponse> ImportCsvAsync(string? fileName, byte[] content, CancellationToken ct = default)
    {
        var parsed = StatementCsvParser.Parse(content);
        if (!parsed.IsValid)
            throw new ImportRejectedException(parsed.Errors
                .Take(MaxReportedErrors)
                .Select(e => $"Satır {e.LineNumber}: {e.Message}")
                .ToList());

        return await StoreAsync("Csv", fileName, parsed.Lines, ct);
    }

    /// <remarks>
    /// Generated lines go through the same fingerprinting as uploaded files, so calling this twice
    /// with the same seed adds nothing the second time.
    /// </remarks>
    public async Task<ImportResultResponse> GenerateDemoHistoryAsync(int months, int seed, CancellationToken ct = default)
    {
        var to = BusinessDate.Today(_time).AddDays(-1);
        var from = new DateOnly(to.Year, to.Month, 1).AddMonths(-(months - 1));

        var lines = SyntheticHistoryGenerator.Generate(from, to, seed)
            .Select((t, i) => new StatementLine(i + 1, t.Date, t.Description, t.Amount, t.Category))
            .ToList();

        return await StoreAsync("Synthetic", fileName: null, lines, ct);
    }

    private async Task<ImportResultResponse> StoreAsync(
        string source, string? fileName, IReadOnlyList<StatementLine> lines, CancellationToken ct)
    {
        var fingerprints = TransactionFingerprint.For(lines);
        var transactions = lines
            .Select((line, i) => new HistoryTransaction(
                line.Date, line.Description, line.Amount, StatementCurrency, line.Category, fingerprints[i]))
            .ToList();

        var outcome = await _history.ImportAsync(_currentUser.UserId, source, fileName, transactions, ct);

        return new ImportResultResponse(outcome.BatchId, outcome.TotalRows, outcome.ImportedRows, outcome.DuplicateRows);
    }

    public async Task<PagedResponse<HistoryTransactionResponse>> ListAsync(
        DateOnly? from, DateOnly? to, int page, int pageSize, CancellationToken ct = default)
    {
        if (from > to)
            throw new BadRequestException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");

        var (items, total) = await _history.ListAsync(_currentUser.UserId, from, to, page, pageSize, ct);

        return new PagedResponse<HistoryTransactionResponse>(
            items.Select(t => new HistoryTransactionResponse(t.Id, t.Date, t.Description, t.Amount, t.CurrencyCode, t.Category)).ToList(),
            page,
            pageSize,
            total);
    }
}
