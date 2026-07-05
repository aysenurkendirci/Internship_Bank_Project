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

    // ✅ Hedefe para ekleme (Add Contribution)
    [HttpPost("savings-goals/{goalId:long}/contributions")]
    public async Task<IActionResult> AddGoalContribution(
        [FromRoute] long goalId,
        [FromBody] AddGoalContributionRequest req,
        CancellationToken ct)
    {
        if (req.Amount <= 0)
            return BadRequest(new { Message = "Tutar 0'dan büyük olmalı." });

        await _service.AddGoalContributionAsync(goalId, req.Amount, ct);
        return NoContent();
    }

    [HttpDelete("savings-goals/{goalId:long}")]
    public async Task<IActionResult> DeleteSavingsGoal([FromRoute] long goalId, CancellationToken ct)
    {
        await _service.DeleteSavingsGoalAsync(goalId, ct);
        return NoContent();
    }
}

public sealed record AddGoalContributionRequest(decimal Amount);