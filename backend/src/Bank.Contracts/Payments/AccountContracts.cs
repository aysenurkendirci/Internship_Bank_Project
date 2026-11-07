using System.ComponentModel.DataAnnotations;

namespace Bank.Contracts.Payments;

public sealed record AccountResponse(
    long Id,
    string Iban,
    string AccountType,
    string CurrencyCode,
    decimal Balance,
    string Status
);

public sealed record OpenAccountRequest(
    [Required(ErrorMessage = "Para birimi zorunludur.")]
    [AllowedValues("TRY", "USD", "EUR", ErrorMessage = "Para birimi TRY, USD veya EUR olmalıdır.")]
    string CurrencyCode
);

/// <summary>One line of an account statement, read from the ledger.</summary>
/// <param name="Side"><c>Debit</c> when money left the account, <c>Credit</c> when it arrived.</param>
/// <param name="CounterpartyIban">The other account of the transfer.</param>
public sealed record StatementEntryResponse(
    long EntryId,
    long TransferId,
    string Side,
    decimal Amount,
    string CurrencyCode,
    decimal BalanceAfter,
    string Description,
    string CounterpartyIban,
    DateTime CreatedAt
);
