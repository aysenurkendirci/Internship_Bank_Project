using Bank.Application.Features.Cards;
using Bank.Application.Features.Dashboard;
using Bank.Contracts.Cards;
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

    [HttpPut("{id:long}/settings")]
    public async Task<ActionResult<CardSettingsResponse>> UpdateSettings(
        long id, [FromBody] CardSettingsRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateCardSettingsCommand(id, request), ct));

    [HttpPost("{id:long}/block")]
    public async Task<ActionResult<CardSettingsResponse>> Block(long id, CancellationToken ct)
        => Ok(await _sender.Send(new SetCardBlockedCommand(id, Blocked: true), ct));

    [HttpPost("{id:long}/unblock")]
    public async Task<ActionResult<CardSettingsResponse>> Unblock(long id, CancellationToken ct)
        => Ok(await _sender.Send(new SetCardBlockedCommand(id, Blocked: false), ct));
}
