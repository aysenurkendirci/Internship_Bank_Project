namespace Bank.Application.Abstractions.Security;

public interface ITokenGenerator
{
    string Generate(long userId, string? email);
}
//cd backend/src/Bank.Api
//dotnet run

//cd frontend
//ng serve