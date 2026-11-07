using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface IRecurringCashFlowService
{
    Task<IReadOnlyList<RecurringCashFlowResponse>> ListAsync(string? kind, CancellationToken ct = default);
    Task<RecurringCashFlowResponse> GetAsync(long id, CancellationToken ct = default);
    Task<RecurringCashFlowResponse> CreateAsync(RecurringCashFlowRequest request, CancellationToken ct = default);
    Task<RecurringCashFlowResponse> UpdateAsync(long id, RecurringCashFlowRequest request, string version, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}
