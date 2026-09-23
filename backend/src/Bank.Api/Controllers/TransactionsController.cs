using Bank.Api.Security;
using Bank.Application.Abstractions.Security;
using Bank.Application.Features.Transactions.Commands.TransferMoney;
using Bank.Contracts.Transactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TransactionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUser _currentUser;

    public TransactionsController(ISender sender, ICurrentUser currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request, CancellationToken cancellationToken)
    {
        var command = new TransferMoneyCommand(
            _currentUser.UserId,
            request.SenderAccountId,
            request.ReceiverAccountId,
            request.Amount,
            request.Description
        );

        var result = await _sender.Send(command, cancellationToken);
        
        return Ok(new { Success = result, Message = "Transfer başarıyla gerçekleştirildi." });
    }
}
