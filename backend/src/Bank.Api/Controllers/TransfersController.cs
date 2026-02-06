using Bank.Application.Abstractions.Repositories;
using Bank.Contracts.Transfers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize]
public sealed class TransfersController : ControllerBase
{
    private readonly ITransfersRepository _repo;
    public TransfersController(ITransfersRepository repo) => _repo = repo;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTransferRequest req, CancellationToken ct)
    {
        if (req.Amount <= 0) return BadRequest("Amount must be > 0.");

        var toAccount = req.ToAccountId is not null;
        var toCard = req.ToCardId is not null;

        if (toAccount == toCard)
            return BadRequest("Provide either ToAccountId or ToCardId.");

        var res = await _repo.CreateAsync(req, ct);
        return Ok(res);
    }
}
