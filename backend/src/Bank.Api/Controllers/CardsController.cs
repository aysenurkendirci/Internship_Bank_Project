using Bank.Application.Abstractions.Services;
using Bank.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public sealed class CardsController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public CardsController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CardItem>>> GetMyCards(CancellationToken ct)
    {
        var sub =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("sub") ??
            User.FindFirstValue("userId");

        if (!long.TryParse(sub, out var userId))
            return Unauthorized("Invalid user id claim.");

       var dashboard = await _dashboard.GetDashboardAsync(ct);
         var dash = await _dashboard.GetDashboardAsync(ct);
        return Ok(dash.Cards);
    }
}
