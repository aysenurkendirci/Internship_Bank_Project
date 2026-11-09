using Bank.Api.Security;
using Bank.Application.Features.Privacy;
using Bank.Application.Features.Profile;
using Bank.Contracts.Account;
using Bank.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly ISender _sender;

    public MeController(ISender sender) => _sender = sender;

    [HttpGet]
    public async Task<ActionResult<ProfileResponse>> Profile(CancellationToken ct)
        => Ok(await _sender.Send(new GetProfileQuery(), ct));

    [HttpPut]
    public async Task<ActionResult<ProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
        => Ok(await _sender.Send(new UpdateProfileCommand(request), ct));

    /// <summary>Changes the password and ends every other session; this one continues with a new refresh cookie.</summary>
    [HttpPut("password")]
    [EnableRateLimiting(AuthRateLimiting.Policy)]
    public async Task<ActionResult<AuthResponse>> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new ChangePasswordCommand(request), ct);
        RefreshCookie.Write(Response, result.RefreshToken);
        return Ok(result.Response);
    }

    /// <summary>Downloads everything stored about the signed-in user as a JSON file (KVKK right of access).</summary>
    [HttpGet("data-export")]
    public async Task<ActionResult<PersonalDataExport>> ExportPersonalData(CancellationToken ct)
    {
        Response.Headers.ContentDisposition = "attachment; filename=\"kisisel-verilerim.json\"";
        return Ok(await _sender.Send(new ExportPersonalDataQuery(), ct));
    }

    /// <summary>
    /// Erases the signed-in customer (KVKK right to erasure): personal finance data is deleted with its history,
    /// accounts are closed and the person is removed from the user record. Needs the password again and an empty
    /// balance. Final.
    /// </summary>
    [HttpDelete]
    [EnableRateLimiting(AuthRateLimiting.Policy)]
    public async Task<IActionResult> EraseAccount([FromBody] EraseAccountRequest request, CancellationToken ct)
    {
        await _sender.Send(new EraseAccountCommand(request.Password), ct);
        RefreshCookie.Clear(Response);
        return NoContent();
    }
}
