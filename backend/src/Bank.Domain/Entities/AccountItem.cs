namespace Bank.Contracts.Dashboard;

public sealed record AccountItem(
    long AccountId,
    string Type,
    string IbanMasked,
    decimal Balance,
    string Status
);
