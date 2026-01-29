namespace Bank.Contracts.Auth;

public sealed record AuthResponse(long UserId, string FullName, string Token);
