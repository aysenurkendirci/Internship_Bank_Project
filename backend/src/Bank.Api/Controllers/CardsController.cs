using Bank.Application.Abstractions.Services;
using Bank.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public sealed class CardsController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public CardsController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CardItem>>> GetMyCards(CancellationToken ct)
    {
        var dashboard = await _dashboard.GetDashboardAsync(ct);
        return Ok(dashboard.Cards);
    }
}
