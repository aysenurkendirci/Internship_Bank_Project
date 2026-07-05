using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;
using Bank.Application.Abstractions.Investments;

namespace Bank.Infrastructure.Investments;

public sealed class VakifbankInvestmentsService : IInvestmentsService
{
    private readonly HttpClient _http;
    private readonly VakifbankOptions _opt;

    public VakifbankInvestmentsService(HttpClient http, IOptions<VakifbankOptions> options)
    {
        _http = http;
        _opt = options.Value;
    }

    public async Task<object> GetMarketRatesAsync(DateTime? validityDate, CancellationToken ct)
{
    var formattedDate = (validityDate ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss+03:00");
    var json = $"{{\"ValidityDate\": \"{formattedDate}\"}}";
    var content = new StringContent(json, Encoding.UTF8, "application/json");
    using var res = await _http.PostAsync(_opt.CurrencyRatesPath, content, ct);
    return JsonSerializer.Deserialize<object>(await res.Content.ReadAsStringAsync(ct))!;
}

public async Task<object> GetGoldPricesAsync(DateTime? priceDate, CancellationToken ct)
{
    var formattedDate = (priceDate ?? DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss+03:00");
    var json = $"{{\"PriceDate\": \"{formattedDate}\"}}";
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    using var res = await _http.PostAsync(_opt.GoldPricesPath, content, ct);
    var raw = await res.Content.ReadAsStringAsync(ct);
    if (!res.IsSuccessStatusCode) throw new Exception($"VAKIFBANK ALTIN HATASI: {raw}");
    return JsonSerializer.Deserialize<object>(raw)!;
}

public async Task<object> ConvertAsync(ConvertCurrencyRequest request, CancellationToken ct)
{
    var sourceCode = request.SourceCurrencyCode == "TRY" ? "TL" : request.SourceCurrencyCode;
    var targetCode = request.TargetCurrencyCode == "TRY" ? "TL" : request.TargetCurrencyCode;

    var json = $"{{\"SourceCurrencyCode\": \"{sourceCode}\", \"SourceAmount\": \"{request.SourceAmount}\", \"TargetCurrencyCode\": \"{targetCode}\"}}";
    
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    using var res = await _http.PostAsync(_opt.CurrencyCalculatorPath, content, ct);
    var raw = await res.Content.ReadAsStringAsync(ct);

    if (!res.IsSuccessStatusCode) throw new Exception($"VAKIFBANK HESAP HATASI: {raw}");
    
    return JsonSerializer.Deserialize<object>(raw)!;
}
}