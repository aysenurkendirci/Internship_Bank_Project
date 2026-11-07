using System.ComponentModel.DataAnnotations;
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers.Finance;

[ApiController]
[Route("api/finance/projection")]
[Authorize]
public sealed class ProjectionController : ControllerBase
{
    private readonly IProjectionService _projection;

    public ProjectionController(IProjectionService projection) => _projection = projection;

    /// <summary>Cash-flow timeline and monthly summary from tomorrow for the given number of months.</summary>
    [HttpGet]
    public async Task<ActionResult<ProjectionResponse>> Get(
        [FromQuery, Range(1, 12, ErrorMessage = "Projeksiyon süresi 1 ile 12 ay arasında olmalıdır.")] int months = 6,
        CancellationToken ct = default)
        => Ok(await _projection.ProjectAsync(months, ct));
}
