using Bank.Application.Features.RecurringCashFlows;
using Bank.Contracts.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/recurring-cash-flows")]
[Authorize]
public sealed class RecurringCashFlowsController : ControllerBase
{
    private readonly ISender _sender;

    public RecurringCashFlowsController(ISender sender) => _sender = sender;

    /// <param name="kind">Optional filter, e.g. <c>Subscription</c> to review all subscriptions.</param>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RecurringCashFlowResponse>>> List([FromQuery] string? kind, CancellationToken ct)
        => Ok(await _sender.Send(new ListRecurringCashFlowsQuery(kind), ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<RecurringCashFlowResponse>> Get(long id, CancellationToken ct)
        => Ok(await _sender.Send(new GetRecurringCashFlowQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<RecurringCashFlowResponse>> Create(
        [FromBody] RecurringCashFlowRequest request, CancellationToken ct)
    {
        var created = await _sender.Send(new CreateRecurringCashFlowCommand(request), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<RecurringCashFlowResponse>> Update(
        long id,
        [FromBody] RecurringCashFlowRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return this.VersionRequired();

        return Ok(await _sender.Send(new UpdateRecurringCashFlowCommand(id, request, ifMatch), ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _sender.Send(new DeleteRecurringCashFlowCommand(id), ct);
        return NoContent();
    }
}
