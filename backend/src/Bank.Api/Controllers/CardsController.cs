using Bank.Api.Security;
using Bank.Application.Abstractions.Security;
using Bank.Application.Features.Cards.Commands.UpdateSettings;
using Bank.Contracts.Cards;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CardsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public CardsController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [HttpPatch("{cardId}/settings")]
    public async Task<IActionResult> UpdateSettings(long cardId, [FromBody] UpdateCardSettingsRequest request, CancellationToken cancellationToken)
    {
        var command = new UpdateCardSettingsCommand(
            _currentUser.UserId,
            cardId,
            request.IsContactlessEnabled,
            request.IsOnlineEnabled
        );

        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new { Success = result, Message = "Kart ayarları başarıyla güncellendi." });
    }
}
