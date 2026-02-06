namespace Bank.Contracts.Transfers;

public sealed record TransferResponse
{
    public string Status { get; init; } = "SUCCESS";
}
