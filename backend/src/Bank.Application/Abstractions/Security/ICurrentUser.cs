namespace Bank.Application.Abstractions.Security;

/// <summary>Who is making the request. Implemented by the API, which is the only layer that knows about HTTP.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    long UserId { get; }
}
