using Bank.Application.Features.Dashboard;
using Bank.Contracts.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> Get(CancellationToken ct)
        => Ok(await _sender.Send(new GetDashboardQuery(), ct));
}
