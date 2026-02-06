using Bank.Application.Abstractions.Services;
using Bank.Contracts.Dashboard;   
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class TransactionsController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public TransactionsController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<TransactionItem>>> GetRecent(CancellationToken ct)
    {
        var items = await _dashboard.GetRecentTransactionsAsync(ct);
        return Ok(items);
    }
}
