
using Bank.Application.Abstractions.Services;
using Bank.Contracts.Auth;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth; //private değişkenlerin başına _ eklenir.

    public AuthController(IAuthService auth) => _auth = auth;
    //public AuthController(IAuthService auth)
//{
  //  _auth = auth;
//} aynı işi yapar.

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
        => Ok(await _auth.LoginAsync(req));

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
        => Ok(await _auth.RegisterAsync(req));
}
