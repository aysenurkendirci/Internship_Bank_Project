namespace Bank.Contracts.Transactions;

public sealed record TransferRequest(
    long SenderAccountId,
    long ReceiverAccountId,
    decimal Amount,
    string Description
);
