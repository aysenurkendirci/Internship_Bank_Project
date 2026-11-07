using Bank.Contracts.Finance;

namespace Bank.Application.Abstractions.Services;

public interface IAnalyticsService
{
    Task<CategoryTrendsResponse> CategoryTrendsAsync(int months, CancellationToken ct = default);
    Task<IReadOnlyList<RecurringPaymentResponse>> RecurringPaymentsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AnomalyResponse>> AnomaliesAsync(int months, CancellationToken ct = default);
    Task<SpendingForecastResponse> ForecastAsync(int months, CancellationToken ct = default);
}
