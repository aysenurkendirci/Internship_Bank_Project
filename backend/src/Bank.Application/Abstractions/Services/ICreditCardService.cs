using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface ICreditCardService
{
    Task<IReadOnlyList<CreditCardResponse>> ListAsync(CancellationToken ct = default);
    Task<CreditCardResponse> GetAsync(long id, CancellationToken ct = default);
    Task<CreditCardResponse> CreateAsync(CreditCardRequest request, CancellationToken ct = default);
    Task<CreditCardResponse> UpdateAsync(long id, CreditCardRequest request, string version, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
