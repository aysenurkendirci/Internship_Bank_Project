using Bank.Application.Features.Planning;
using Bank.Contracts.Finance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/projection")]
[Authorize]
public sealed class ProjectionController : ControllerBase
{
    private readonly ISender _sender;

    public ProjectionController(ISender sender) => _sender = sender;

    /// <summary>Cash-flow timeline and monthly summary from tomorrow for the given number of months.</summary>
    [HttpGet]
    public async Task<ActionResult<ProjectionResponse>> Get([FromQuery] int months = 6, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetProjectionQuery(months), ct));
}
