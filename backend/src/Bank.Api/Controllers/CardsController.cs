using Bank.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/cards")]
public sealed class CardsController : ControllerBase
{
    private readonly ICardsRepository _repo;
    public CardsController(ICardsRepository repo) => _repo = repo;

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var res = await _repo.GetCardDetailAsync(id, ct);
        return res is null ? NotFound() : Ok(res);
    }
}
