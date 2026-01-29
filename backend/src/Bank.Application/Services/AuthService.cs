using Bank.Application.Abstractions;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Contracts.Auth;
using Bank.Application.Abstractions.Services;
namespace Bank.Application.Services; // Program.cs 3. satırın çalışması için bu şart
public sealed class AuthService : IAuthService
{
    private readonly IAuthRepository _repo;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenGenerator _token;

    public AuthService(IAuthRepository repo, IPasswordHasher hasher, ITokenGenerator token)
    {
        _repo = repo; _hasher = hasher; _token = token;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        var passwordHash = _hasher.Hash(req.Password);
        var user = await _repo.CreateUserAsync(req, passwordHash);

        var jwt = _token.Generate(user.UserId, user.Email); //token oluşturma
        return new AuthResponse(user.UserId, $"{user.FirstName} {user.LastName}", jwt);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req)
    {
        var user = await _repo.GetUserByIdentifierAsync(req.Identifier);//kullanıcı var mı yok mu kontrolü
        var cred = await _repo.GetCredentialsAsync(user.UserId);//kullanıcının şifre bilgilerini al

        if (!_hasher.Verify(req.Password, cred.PasswordHash))//şifre doğrulama
            throw new UnauthorizedAccessException("Invalid credentials.");

        await _repo.UpdateLastLoginAsync(user.UserId);

        var jwt = _token.Generate(user.UserId, user.Email);
        return new AuthResponse(user.UserId, $"{user.FirstName} {user.LastName}", jwt);
    }
}
