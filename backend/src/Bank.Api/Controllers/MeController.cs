using Bank.Application.Services;
using Bank.Contracts.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly IPersonalDataService _personalData;

    public MeController(IPersonalDataService personalData) => _personalData = personalData;

    /// <summary>Downloads everything stored about the signed-in user as a JSON file (KVKK right of access).</summary>
    [HttpGet("data-export")]
    public async Task<ActionResult<PersonalDataExport>> ExportPersonalData(CancellationToken ct)
    {
        Response.Headers.ContentDisposition = "attachment; filename=\"kisisel-verilerim.json\"";
        return Ok(await _personalData.ExportAsync(ct));
    }
}
