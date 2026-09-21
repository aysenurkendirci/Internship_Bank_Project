using MediatR;
using Bank.Contracts.Auth;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Features.Auth.Commands.Login;

// Handler: MediatR tarafından yakalanıp çalıştırılacak olan asıl işçidir.
// LoginCommand geldiğinde bu sınıf devreye girer ve işi halleder.
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher, 
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Email'i Domain objesine dönüştür (Validasyon)
        var email = Email.Create(request.Email);

        // 2. Veritabanından kullanıcıyı bul
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            throw new DomainException("Geçersiz e-posta veya şifre."); // Güvenlik: Hangisinin yanlış olduğunu söylemiyoruz
        }

        // 3. Şifreyi doğrula
        bool isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new DomainException("Geçersiz e-posta veya şifre.");
        }

        // 4. Token üret ve dön
        var token = _tokenGenerator.Generate(user.Id, user.Email.Value);

        return new AuthResponse(
            user.Id, 
            $"{user.FirstName} {user.LastName}", 
            token);
    }
}
