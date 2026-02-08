using Bank.Application.Services;
using Bank.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken ct)
        => Ok(await _service.GetDashboardAsync(ct));

    [HttpGet("savings-goals")]
    public async Task<ActionResult<IReadOnlyList<SavingsGoalItem>>> GetSavingsGoals(CancellationToken ct)
        => Ok(await _service.GetSavingsGoalsAsync(ct));

    [HttpPost("savings-goals")]
    public async Task<IActionResult> CreateSavingsGoal([FromBody] CreateSavingsGoalRequest req, CancellationToken ct)
    {
        await _service.CreateSavingsGoalAsync(req, ct);
        return NoContent();
    }
}
