using Bank.Application.Features.CreditCards;
using Bank.Contracts.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/credit-cards")]
[Authorize]
public sealed class CreditCardsController : ControllerBase
{
    private readonly ISender _sender;

    public CreditCardsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> List(CancellationToken ct)
        => Ok(await _sender.Send(new ListCreditCardsQuery(), ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CreditCardResponse>> Get(long id, CancellationToken ct)
        => Ok(await _sender.Send(new GetCreditCardQuery(id), ct));

    [HttpPost]
    public async Task<ActionResult<CreditCardResponse>> Create([FromBody] CreditCardRequest request, CancellationToken ct)
    {
        var created = await _sender.Send(new CreateCreditCardCommand(request), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <remarks>
    /// Requires the card's current <c>version</c> in the If-Match header. A missing header is answered
    /// with 428 so clients cannot accidentally overwrite changes they never saw.
    /// </remarks>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<CreditCardResponse>> Update(
        long id,
        [FromBody] CreditCardRequest request,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ifMatch))
            return this.VersionRequired();

        return Ok(await _sender.Send(new UpdateCreditCardCommand(id, request, ifMatch), ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _sender.Send(new DeleteCreditCardCommand(id), ct);
        return NoContent();
    }
}
