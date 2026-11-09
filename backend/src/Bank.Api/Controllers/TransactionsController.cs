using Bank.Application.Features.Dashboard;
using Bank.Contracts.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

/// <summary>The latest account movements, taken from the dashboard read model.</summary>
[ApiController]
[Route("api/transactions")]
[Authorize]
public sealed class TransactionsController : ControllerBase
{
    private readonly ISender _sender;

    public TransactionsController(ISender sender) => _sender = sender;

    [HttpGet("recent")]
    public async Task<ActionResult<IReadOnlyList<TransactionItem>>> GetRecent(CancellationToken ct)
        => Ok((await _sender.Send(new GetDashboardQuery(), ct)).RecentTransactions);
}
