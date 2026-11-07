using Bank.Application.Abstractions.Services;
using Bank.Contracts.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/credit-cards")]
[Authorize]
public sealed class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _cards;

    public CreditCardsController(ICreditCardService cards) => _cards = cards;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CreditCardResponse>>> List(CancellationToken ct)
        => Ok(await _cards.ListAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CreditCardResponse>> Get(long id, CancellationToken ct)
        => Ok(await _cards.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<CreditCardResponse>> Create([FromBody] CreditCardRequest request, CancellationToken ct)
    {
        var created = await _cards.CreateAsync(request, ct);
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
            return Problem(statusCode: StatusCodes.Status428PreconditionRequired,
                           title: "Kayıt sürümü gerekli",
                           detail: "Güncelleme için If-Match başlığında kaydın version değeri gönderilmelidir.");

        return Ok(await _cards.UpdateAsync(id, request, ifMatch, ct));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _cards.DeleteAsync(id, ct);
        return NoContent();
    }
}
