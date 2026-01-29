namespace Bank.Contracts.Auth;

public sealed record RegisterRequest(
    string TcNo,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Password,
    string Membership
);
