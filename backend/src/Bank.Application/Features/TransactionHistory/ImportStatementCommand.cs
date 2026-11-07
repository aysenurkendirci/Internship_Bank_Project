using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common.Exceptions;
using Bank.Application.Imports;
using Bank.Contracts.Finance;
using MediatR;

namespace Bank.Application.Features.TransactionHistory;

/// <summary>
/// Imports a bank statement CSV. Lines already imported are skipped and counted as duplicates,
/// so uploading the same or an overlapping statement again is safe.
/// </summary>
public sealed record ImportStatementCommand(string? FileName, byte[] Content) : IRequest<ImportResultResponse>;

/// <remarks>
/// All or nothing: a file with any invalid line is rejected as a whole. A partial import would
/// leave the user guessing which lines made it in, and fixing the file and re-uploading would
/// then depend on duplicate detection to clean up after it.
/// </remarks>
public sealed class ImportStatementCommandHandler : IRequestHandler<ImportStatementCommand, ImportResultResponse>
{
    private const int MaxReportedErrors = 50;

    private readonly ITransactionHistoryRepository _history;
    private readonly ICurrentUser _currentUser;

    public ImportStatementCommandHandler(ITransactionHistoryRepository history, ICurrentUser currentUser)
    {
        _history = history;
        _currentUser = currentUser;
    }

    public async Task<ImportResultResponse> Handle(ImportStatementCommand command, CancellationToken ct)
    {
        var parsed = StatementCsvParser.Parse(command.Content);
        if (!parsed.IsValid)
            throw new ImportRejectedException(parsed.Errors
                .Take(MaxReportedErrors)
                .Select(e => $"Satır {e.LineNumber}: {e.Message}")
                .ToList());

        return await HistoryImport.StoreAsync(_history, _currentUser.UserId, "Csv", command.FileName, parsed.Lines, ct);
    }
}
