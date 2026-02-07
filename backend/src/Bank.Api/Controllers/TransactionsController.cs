using Bank.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionsRepository _repo;
    public TransactionsController(ITransactionsRepository repo) => _repo = repo;

    [HttpGet("recent")]
    public async Task<IActionResult> Recent(
        [FromQuery] long? accountId,
        [FromQuery] long? cardId,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        var userIdStr =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ??
            User.FindFirstValue("userId");

        if (!long.TryParse(userIdStr, out var userId))
            return Unauthorized("UserId claim not found.");

        // Aynı anda ikisi gelmesin
        if (accountId is not null && cardId is not null)
            return BadRequest("Send either accountId or cardId, not both.");

        var res = await _repo.GetRecentAsync(userId, accountId, cardId, take, ct);
        return Ok(res);
    }
}
