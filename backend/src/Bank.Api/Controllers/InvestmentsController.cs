using Bank.Application.Abstractions.Investments;
using Bank.Contracts.Investments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/investments")]
[Authorize]
public sealed class InvestmentsController : ControllerBase
{
    private readonly IInvestmentsService _service;
    public InvestmentsController(IInvestmentsService service) => _service = service;

    [HttpPost("market-rates")] // POST yaptık (405 hatasını çözer)
    public async Task<IActionResult> GetMarketRates(CancellationToken ct)
    {
        var result = await _service.GetMarketRatesAsync(null, ct);
        return Ok(result);
    }

    [HttpPost("gold-prices")] // POST yaptık (405 hatasını çözer)
    public async Task<IActionResult> GetGoldPrices(CancellationToken ct)
    {
        var result = await _service.GetGoldPricesAsync(null, ct);
        return Ok(result);
    }

    [HttpPost("convert")]
    public async Task<IActionResult> Convert([FromBody] Bank.Contracts.Investments.ConvertCurrencyRequest req, CancellationToken ct)
    {
        var appReq = new Bank.Application.Abstractions.Investments.ConvertCurrencyRequest
        {
            SourceCurrencyCode = req.SourceCurrencyCode,
            SourceAmount = req.SourceAmount,
            TargetCurrencyCode = req.TargetCurrencyCode
        };

        var result = await _service.ConvertAsync(appReq, ct);
        return Ok(result);
    }
}