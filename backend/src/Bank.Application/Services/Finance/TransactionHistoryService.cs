using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Abstractions.Services;
using Bank.Application.Common.Exceptions;
using Bank.Application.Imports;
using Bank.Contracts.Finance;

namespace Bank.Application.Services.Finance;

public sealed class TransactionHistoryService : ITransactionHistoryService
{
    // Statements carry no currency column; files are treated as TRY statements.
    private const string StatementCurrency = "TRY";
    private const int MaxReportedErrors = 50;

    private readonly ITransactionHistoryRepository _history;
    private readonly ICurrentUser _currentUser;

    public TransactionHistoryService(ITransactionHistoryRepository history, ICurrentUser currentUser)
    {
        _history = history;
        _currentUser = currentUser;
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

        var fingerprints = TransactionFingerprint.For(parsed.Lines);
        var transactions = parsed.Lines
            .Select((line, i) => new HistoryTransaction(
                line.Date, line.Description, line.Amount, StatementCurrency, line.Category, fingerprints[i]))
            .ToList();

        var outcome = await _history.ImportAsync(_currentUser.UserId, "Csv", fileName, transactions, ct);

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
