using Bank.Application.Features.InstallmentPlans;
using Bank.Contracts.Finance;
using MediatR;
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
    private readonly ISender _sender;

    public InstallmentPlansController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InstallmentPlanResponse>>> List(CancellationToken ct)
        => Ok(await _sender.Send(new ListInstallmentPlansQuery(), ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<InstallmentPlanResponse>> Get(long id, CancellationToken ct)
        => Ok(await _sender.Send(new GetInstallmentPlanQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<InstallmentPlanResponse>> Create(
        [FromBody] InstallmentPlanRequest request, CancellationToken ct)
    {
        var created = await _sender.Send(new CreateInstallmentPlanCommand(request), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _sender.Send(new DeleteInstallmentPlanCommand(id), ct);
        return NoContent();
    }
}
