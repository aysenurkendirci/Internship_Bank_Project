using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Payments;
using Bank.Domain.Accounts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bank.Application.Features.Accounts;

/// <summary>Opens an empty account in the given currency: a demand account for TRY, a foreign currency account otherwise.</summary>
public sealed record OpenAccountCommand(OpenAccountRequest Account) : IRequest<AccountResponse>;

public sealed class OpenAccountCommandHandler : IRequestHandler<OpenAccountCommand, AccountResponse>
{
    /// <summary>
    /// The bank's five-digit code inside its IBANs. Real codes are assigned by the central bank; this one is
    /// deliberately not assigned to any bank, so a generated IBAN can never point at a real account.
    /// </summary>
    public const string BankCode = "99999";

    private readonly IAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<OpenAccountCommandHandler> _logger;

    public OpenAccountCommandHandler(IAccountRepository accounts, ICurrentUser currentUser, ILogger<OpenAccountCommandHandler> logger)
    {
        _accounts = accounts;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AccountResponse> Handle(OpenAccountCommand command, CancellationToken ct)
    {
        var currency = command.Account.CurrencyCode;
        var accountType = currency == "TRY" ? "Demand" : "ForeignCurrency";
        var iban = Iban.For(BankCode, await _accounts.NextAccountNumberAsync(ct));

        var opened = await _accounts.OpenAsync(_currentUser.UserId, accountType, currency, iban, ct);

        _logger.LogInformation("User {UserId} opened account {AccountId} in {Currency}", _currentUser.UserId, opened.AccountId, currency);
        return AccountMapping.ToResponse(opened);
    }
}
