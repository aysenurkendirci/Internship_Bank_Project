namespace Bank.Contracts.Transactions;

public sealed record TransactionItem(
    long TxId,
    string Title,
    string Category,
    decimal Amount,
    string Direction,
    DateTime CreatedAt
);

