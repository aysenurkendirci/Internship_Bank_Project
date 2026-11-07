using Bank.Application.Features.Privacy;
using Bank.Contracts.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly ISender _sender;

    public MeController(ISender sender) => _sender = sender;

    /// <summary>Downloads everything stored about the signed-in user as a JSON file (KVKK right of access).</summary>
    [HttpGet("data-export")]
    public async Task<ActionResult<PersonalDataExport>> ExportPersonalData(CancellationToken ct)
    {
        Response.Headers.ContentDisposition = "attachment; filename=\"kisisel-verilerim.json\"";
        return Ok(await _sender.Send(new ExportPersonalDataQuery(), ct));
    }
}
