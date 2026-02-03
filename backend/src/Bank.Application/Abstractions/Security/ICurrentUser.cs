namespace Bank.Application.Abstractions.Security;

public interface ICurrentUser
{
   bool IsAuthenticated { get; }
    long UserId { get; }
    string Email { get; }
}
