namespace Bank.Contracts.Accounts;

public sealed record CreateAccountRequest(
    int Type,
    string Currency = "TRY"
);
