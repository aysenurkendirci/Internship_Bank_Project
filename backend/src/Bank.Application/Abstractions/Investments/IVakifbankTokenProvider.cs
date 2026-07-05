namespace Bank.Application.Abstractions.Investments;

public interface IVakifbankTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken ct);
}
