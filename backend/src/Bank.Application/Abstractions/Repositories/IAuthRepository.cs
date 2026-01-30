using Bank.Contracts.Auth;
namespace Bank.Application.Abstractions.Repositories;
public interface IAuthRepository
{
    Task<UserRow> GetUserByTcAsync(string tcNo);//kullanıcı var mı yok mu kontrol
    Task UpdateLastLoginAsync(long userId);//Sadece şifre doğrulaması yapılacağı zaman çağrılır 
    Task<UserRow> CreateUserAsync(RegisterRequest req, string passwordHash);//kayıt

    Task<CredentialRow> GetCredentialsAsync(long userId);
}

public sealed record UserRow(long UserId, string Email, string FirstName, string LastName); //class yerine record kullanmak sadece veri taşıyıcılığı için vardır
public sealed record CredentialRow(long UserId, string PasswordHash); //sealed miras alınmasını engeller
