using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Application.Common;
using Bank.Application.Imports;
using Bank.Application.Synthetic;
using Bank.Contracts.Finance;
using FluentValidation;
using MediatR;

namespace Bank.Application.Features.TransactionHistory;

/// <summary>
/// Generates a reproducible demo history (salary, bills, subscriptions, everyday spending and a few
/// unusually large purchases) for the last <paramref name="Months"/> months.
/// </summary>
public sealed record GenerateDemoHistoryCommand(int Months, int Seed) : IRequest<ImportResultResponse>;

public sealed class GenerateDemoHistoryCommandValidator : AbstractValidator<GenerateDemoHistoryCommand>
{
    public GenerateDemoHistoryCommandValidator()
    {
        RuleFor(c => c.Months).InclusiveBetween(1, 24).WithMessage("Süre 1 ile 24 ay arasında olmalıdır.");
    }
}

/// <remarks>
/// Generated lines go through the same fingerprinting as uploaded files, so calling this twice
/// with the same seed adds nothing the second time.
/// </remarks>
public sealed class GenerateDemoHistoryCommandHandler : IRequestHandler<GenerateDemoHistoryCommand, ImportResultResponse>
{
    private readonly ITransactionHistoryRepository _history;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public GenerateDemoHistoryCommandHandler(ITransactionHistoryRepository history, ICurrentUser currentUser, TimeProvider time)
    {
        _history = history;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<ImportResultResponse> Handle(GenerateDemoHistoryCommand command, CancellationToken ct)
    {
        var to = BusinessDate.Today(_time).AddDays(-1);
        var from = new DateOnly(to.Year, to.Month, 1).AddMonths(-(command.Months - 1));

        var lines = SyntheticHistoryGenerator.Generate(from, to, command.Seed)
            .Select((t, i) => new StatementLine(i + 1, t.Date, t.Description, t.Amount, t.Category))
            .ToList();

        return await HistoryImport.StoreAsync(_history, _currentUser.UserId, "Synthetic", fileName: null, lines, ct);
    }
}
