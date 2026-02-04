using Bank.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountsRepository _repo;
    public AccountsController(IAccountsRepository repo) => _repo = repo;

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id, CancellationToken ct)
    {
        var res = await _repo.GetAccountDetailAsync(id, ct);
        return res is null ? NotFound() : Ok(res);
    }
}
