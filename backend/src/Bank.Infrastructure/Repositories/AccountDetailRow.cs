namespace Bank.Infrastructure.Repositories;

public sealed class AccountDetailRow
{
    public long AccountId { get; set; }
    public string Type { get; set; } = default!;
    public string Iban { get; set; } = default!;
    public decimal Balance { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public string OwnerFirstName { get; set; } = default!;
    public string Membership { get; set; } = default!;
}
