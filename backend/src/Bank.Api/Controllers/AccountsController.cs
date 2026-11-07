using Bank.Application.Features.Accounts;
using Bank.Contracts.Finance;
using Bank.Contracts.Payments;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public sealed class AccountsController : ControllerBase
{
    private readonly ISender _sender;

    public AccountsController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> List(CancellationToken ct)
        => Ok(await _sender.Send(new ListAccountsQuery(), ct));

    /// <summary>Opens an empty account with a newly generated IBAN.</summary>
    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Open([FromBody] OpenAccountRequest request, CancellationToken ct)
    {
        var opened = await _sender.Send(new OpenAccountCommand(request), ct);
        return Created($"/api/accounts/{opened.Id}/statement", opened);
    }

    /// <summary>The account's movements as recorded in the ledger, newest first.</summary>
    [HttpGet("{id:long}/statement")]
    public async Task<ActionResult<PagedResponse<StatementEntryResponse>>> Statement(
        long id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await _sender.Send(new GetAccountStatementQuery(id, page, pageSize), ct));
}
