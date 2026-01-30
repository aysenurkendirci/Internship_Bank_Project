namespace Bank.Contracts.Auth;

public sealed record LoginRequest(
    string TcNo,
    string Password
);
