using Bank.Application.Features.Analytics;
using Bank.Contracts.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

/// <summary>
/// Read-only analysis of the user's transaction history. Describes patterns; never recommends
/// financial products or decisions.
/// </summary>
[ApiController]
[Route("api/finance/analytics")]
[Authorize]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ISender _sender;

    public AnalyticsController(ISender sender) => _sender = sender;

    /// <summary>Spending per category over the last complete months.</summary>
    [HttpGet("category-trends")]
    public async Task<ActionResult<CategoryTrendsResponse>> CategoryTrends([FromQuery] int months = 6, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetCategoryTrendsQuery(months), ct));

    /// <summary>Payments that repeat monthly; untracked ones are candidates to add as recurring items.</summary>
    [HttpGet("recurring-payments")]
    public async Task<ActionResult<IReadOnlyList<RecurringPaymentResponse>>> RecurringPayments(CancellationToken ct)
        => Ok(await _sender.Send(new GetRecurringPaymentsQuery(), ct));

    /// <summary>Unusually large purchases in the last months, each explained against a typical amount.</summary>
    [HttpGet("anomalies")]
    public async Task<ActionResult<IReadOnlyList<AnomalyResponse>>> Anomalies([FromQuery] int months = 6, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetAnomaliesQuery(months), ct));

    /// <summary>Expected everyday spending with a 95% interval; needs at least six months of history.</summary>
    [HttpGet("forecast")]
    public async Task<ActionResult<SpendingForecastResponse>> Forecast([FromQuery] int months = 3, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetSpendingForecastQuery(months), ct));
}
