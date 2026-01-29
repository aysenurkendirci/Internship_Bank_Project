namespace Bank.Contracts.Auth;

public sealed record LoginRequest(string Identifier, string Password);
