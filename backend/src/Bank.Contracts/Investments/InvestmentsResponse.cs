using System.Text.Json.Serialization;

namespace Bank.Contracts.Investments;

public record ConvertCurrencyRequest(string SourceCurrencyCode, decimal SourceAmount, string TargetCurrencyCode);

public sealed record MarketRateItem(
    [property: JsonPropertyName("CurrencyCode")] string Code,
    [property: JsonPropertyName("CurrencyName")] string CurrencyName,
    [property: JsonPropertyName("SaleRate")] string SaleRate,
    [property: JsonPropertyName("PurchaseRate")] string PurchaseRate,
    [property: JsonPropertyName("RateDate")] string RateDate,
    [property: JsonPropertyName("ProductName")] string ProductName
);

public sealed record MarketRatesResponse(IReadOnlyList<MarketRateItem> Currency);

public sealed record ConvertCurrencyResponse(
    [property: JsonPropertyName("TargetCurrencyCode")] string TargetCurrencyCode,
    [property: JsonPropertyName("SaleAmount")] decimal SaleAmount,
    [property: JsonPropertyName("SaleRate")] string SaleRate
);

public sealed record GoldRateItem(
    [property: JsonPropertyName("ProductName")] string ProductName,
    [property: JsonPropertyName("CurrencyCode")] string CurrencyCode,
    [property: JsonPropertyName("SaleRate")] string SaleRate,
    [property: JsonPropertyName("PurchaseRate")] string PurchaseRate,
    [property: JsonPropertyName("RateDate")] string RateDate
);

public sealed record GoldRatesResponse(IReadOnlyList<GoldRateItem> GoldRates);