using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bank.Application.Abstractions.Investments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bank.Infrastructure.Investments;

public sealed class VakifbankTokenProvider : IVakifbankTokenProvider
{
    private readonly HttpClient _http;
    private readonly VakifbankOptions _opt;
    private readonly ILogger<VakifbankTokenProvider> _logger;

    private string? _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public VakifbankTokenProvider(
        HttpClient http,
        IOptions<VakifbankOptions> options,
        ILogger<VakifbankTokenProvider> logger)
    {
        _http = http;
        _opt = options.Value;
        _logger = logger;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken) && DateTimeOffset.UtcNow < _expiresAt.AddSeconds(-30))
            return _cachedToken;

        if (string.IsNullOrWhiteSpace(_opt.ClientId) || string.IsNullOrWhiteSpace(_opt.ClientSecret))
            throw new Exception("Vakifbank ClientId/ClientSecret missing in configuration.");

        var tokenUrl = _opt.TokenPath.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? _opt.TokenPath
            : $"{_opt.BaseUrl.TrimEnd('/')}/{_opt.TokenPath.TrimStart('/')}";

        using var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl);

        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opt.ClientId}:{_opt.ClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["scope"] = string.IsNullOrWhiteSpace(_opt.Scope) ? "oob" : _opt.Scope
        });

        using var res = await _http.SendAsync(req, ct);
        var raw = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError("Vakifbank TOKEN ERROR {Status} - {Body}", (int)res.StatusCode, raw);
            throw new Exception($"Vakifbank TOKEN ERROR {(int)res.StatusCode} - {raw}");
        }

        using var doc = JsonDocument.Parse(raw);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString();
        var expiresIn = doc.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;

        if (string.IsNullOrWhiteSpace(accessToken))
            throw new Exception("Vakifbank token response missing access_token.");

        _cachedToken = accessToken;
        _expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);

        return accessToken;
    }
}
