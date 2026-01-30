using Bank.Application.Abstractions.Services;
using Bank.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst("userId")?.Value;

        if (!long.TryParse(userIdClaim, out var userId))
            return Unauthorized("userId claim not found in token.");

        return Ok(await _dashboard.GetDashboardAsync(userId, ct));
    }
}
