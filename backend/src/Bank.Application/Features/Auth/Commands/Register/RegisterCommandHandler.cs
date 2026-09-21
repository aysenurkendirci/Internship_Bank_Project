using MediatR;
using Bank.Contracts.Auth;
using Bank.Application.Abstractions.Repositories;
using Bank.Application.Abstractions.Security;
using Bank.Domain.Entities;
using Bank.Domain.Exceptions;
using Bank.Domain.ValueObjects;

namespace Bank.Application.Features.Auth.Commands.Register;

// Handler: RegisterCommand (Kayıt Ol) isteği geldiğinde çalışan işçi sınıf.
public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenGenerator _tokenGenerator;

    public RegisterCommandHandler(
        IUserRepository userRepository, 
        IPasswordHasher passwordHasher, 
        ITokenGenerator tokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Email ve TcNo Domain objesine dönüştürülüyor (Validasyon)
        var email = Email.Create(request.Email);
        var tcNo = TcNo.Create(request.TcNo);

        // 2. Bu e-posta veya TC no ile daha önce kayıt olunmuş mu?
        var existingUserByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingUserByEmail is not null)
        {
            throw new DomainException("Bu e-posta adresi ile zaten bir kayıt mevcut.");
        }

        var existingUserByTcNo = await _userRepository.GetByTcNoAsync(tcNo, cancellationToken);
        if (existingUserByTcNo is not null)
        {
            throw new DomainException("Bu TC Kimlik numarası ile zaten bir kayıt mevcut.");
        }

        // 3. Şifreyi şifrele (Hash)
        string passwordHash = _passwordHasher.Hash(request.Password);

        // 4. Yeni User entity'sini oluştur
        var newUser = new User(
            request.TcNo, 
            request.FirstName, 
            request.LastName, 
            request.Email, 
            request.Phone, 
            passwordHash);

        // 5. Veritabanına kaydet
        await _userRepository.AddAsync(newUser, cancellationToken);

        // 6. Kayıt başarılı, direkt giriş yapması için Token üret
        var token = _tokenGenerator.Generate(newUser.Id, newUser.Email.Value);

        return new AuthResponse(
            newUser.Id, 
            $"{newUser.FirstName} {newUser.LastName}", 
            token);
    }
}
