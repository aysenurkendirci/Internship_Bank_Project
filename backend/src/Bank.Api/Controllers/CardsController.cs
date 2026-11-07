using Bank.Application.Features.Dashboard;
using Bank.Contracts.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/cards")]
[Authorize]
public sealed class CardsController : ControllerBase
{
    private readonly ISender _sender;

    public CardsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CardItem>>> GetMyCards(CancellationToken ct)
        => Ok((await _sender.Send(new GetDashboardQuery(), ct)).Cards);
}
