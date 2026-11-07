using Bank.Application.Abstractions.Services;
using Bank.Contracts.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

/// <remarks>
/// No PUT: the installment terms of a purchase cannot change afterwards. A wrongly entered plan is
/// deleted and created again.
/// </remarks>
[ApiController]
[Route("api/finance/installment-plans")]
[Authorize]
public sealed class InstallmentPlansController : ControllerBase
{
    private readonly IInstallmentPlanService _plans;

    public InstallmentPlansController(IInstallmentPlanService plans) => _plans = plans;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InstallmentPlanResponse>>> List(CancellationToken ct)
        => Ok(await _plans.ListAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InstallmentPlanResponse>> Get(long id, CancellationToken ct)
        => Ok(await _plans.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<InstallmentPlanResponse>> Create(
        [FromBody] InstallmentPlanRequest request, CancellationToken ct)
    {
        var created = await _plans.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _plans.DeleteAsync(id, ct);
        return NoContent();
    }
}
