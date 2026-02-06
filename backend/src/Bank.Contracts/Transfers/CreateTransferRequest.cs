namespace Bank.Contracts.Transfers;

public sealed record CreateTransferRequest
{
    public long FromAccountId { get; init; }
    public long? ToAccountId { get; init; }   
    public long? ToCardId { get; init; }     
    public decimal Amount { get; init; }
    public string? Note { get; init; }
}
