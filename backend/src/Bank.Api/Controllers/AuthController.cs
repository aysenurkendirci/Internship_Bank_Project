using Bank.Api.Security;
using Bank.Application.Common.Exceptions;
using Bank.Application.Features.Auth;
using Bank.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Bank.Api.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting(AuthRateLimiting.Policy)]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
        => SignedIn(await _sender.Send(new LoginCommand(request), ct));

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request, CancellationToken ct)
        => SignedIn(await _sender.Send(new RegisterCommand(request), ct));

    /// <summary>
    /// Trades the refresh token cookie for a new access token (and a new cookie). A token that was already used
    /// ends every session of its user.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        if (!RefreshCookie.IsFromTheApp(Request))
            return RefreshCookie.Forbidden(this);

        var result = await _sender.Send(new RefreshSessionCommand(RefreshCookie.Read(Request)), ct);
        if (result is null)
        {
            RefreshCookie.Clear(Response);
            throw new AuthenticationFailedException();
        }

        return SignedIn(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (!RefreshCookie.IsFromTheApp(Request))
            return RefreshCookie.Forbidden(this);

        await _sender.Send(new LogoutCommand(RefreshCookie.Read(Request)), ct);
        RefreshCookie.Clear(Response);
        return NoContent();
    }

    private ActionResult<AuthResponse> SignedIn(AuthResult result)
    {
        RefreshCookie.Write(Response, result.RefreshToken);
        return Ok(result.Response);
    }
}
