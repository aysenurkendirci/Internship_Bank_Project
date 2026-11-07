using Bank.Application.Features.Transfers;
using Bank.Contracts.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController : ControllerBase
{
    private const string ReplayedHeader = "Idempotent-Replayed";

    private readonly ISender _sender;

    public TransfersController(ISender sender) => _sender = sender;

    /// <summary>Sends money to another account at this bank.</summary>
    /// <remarks>
    /// Requires an <c>Idempotency-Key</c> header. Retrying with the same key and body returns the original
    /// transfer (marked with <c>Idempotent-Replayed: true</c>) instead of sending the money again; the same key
    /// with a different body is refused with 422.
    /// </remarks>
    [HttpPost]
    public async Task<ActionResult<TransferResponse>> Send(
        [FromBody] TransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken ct)
    {
        var result = await _sender.Send(new TransferMoneyCommand(request, idempotencyKey), ct);

        if (result.Replayed)
        {
            Response.Headers[ReplayedHeader] = "true";
            return Ok(result.Transfer);
        }

        return CreatedAtAction(nameof(Get), new { id = result.Transfer.Id }, result.Transfer);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<TransferResponse>> Get(long id, CancellationToken ct)
        => Ok(await _sender.Send(new GetTransferQuery(id), ct));
}
