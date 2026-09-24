using Bank.Application.Features.Accounts.Commands.CreateAccount;
using Bank.Contracts.Accounts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Api.Controllers;

[Route("api/accounts")]
[ApiController]
[Authorize] // Sadece giriş yapanlar hesap açabilir
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        // JWT Token içindeki User ID'yi (Kimlik numarasını) Claim'lerden okuyoruz.
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        // MediatR'a göndereceğimiz Command nesnesini hazırlıyoruz.
        // Dışarıdan gelen DTO ile, Token'dan gelen UserId'yi birleştiriyoruz.
        var command = new CreateAccountCommand(userId, request.Type, request.Currency);

        // Command'i Handler'a gönderiyoruz ve sonucunda yeni oluşan hesabın ID'sini alıyoruz.
        var accountId = await _mediator.Send(command, cancellationToken);

        return Ok(new { AccountId = accountId, Message = "Hesap başarıyla oluşturuldu." });
    }
}
