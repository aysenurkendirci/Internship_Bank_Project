namespace Bank.Application.Abstractions.Investments;

public interface IInvestmentsService
{
    Task<object> GetMarketRatesAsync(DateTime? validityDate, CancellationToken ct);
    Task<object> GetGoldPricesAsync(DateTime? priceDate, CancellationToken ct);
    Task<object> ConvertAsync(ConvertCurrencyRequest request, CancellationToken ct);
}
