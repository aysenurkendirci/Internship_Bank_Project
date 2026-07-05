using System.Net.Http.Headers;
using Bank.Application.Abstractions.Investments;

namespace Bank.Infrastructure.Investments;

public sealed class VakifbankAuthHandler : DelegatingHandler
{
    private readonly IVakifbankTokenProvider _tokenProvider;

    public VakifbankAuthHandler(IVakifbankTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var token = await _tokenProvider.GetAccessTokenAsync(ct);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.ParseAdd("application/json");
        return await base.SendAsync(request, ct);
    }
}
